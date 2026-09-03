`timescale 1ns/1ps

module trigger_capture(
    input  wire        clk,
    input  wire        rst_n,
    input  wire        async_in,
    input  wire        active_low,
    input  wire        monitor_enable,
    input  wire [31:0] timeout_ticks,
    input  wire        clear_stats,
    output reg  [31:0] event_count,
    output reg  [31:0] last_width,
    output reg  [31:0] last_period,
    output reg  [31:0] too_narrow_count,
    output reg         timeout_flag,
    output reg         overflow_flag,
    output reg         event_pulse
);
    (* async_reg = "true" *) reg gpio1_sync_ff1;
    (* async_reg = "true" *) reg gpio1_sync_ff2;
    reg active_prev;
    reg [31:0] width_count;
    reg [31:0] period_count;
    reg have_previous;
    wire active_now = active_low ? !gpio1_sync_ff2 : gpio1_sync_ff2;

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            gpio1_sync_ff1 <= 1'b1;
            gpio1_sync_ff2 <= 1'b1;
        end else begin
            gpio1_sync_ff1 <= async_in;
            gpio1_sync_ff2 <= gpio1_sync_ff1;
        end
    end

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            event_count      <= 32'd0;
            last_width       <= 32'd0;
            last_period      <= 32'd0;
            too_narrow_count <= 32'd0;
            timeout_flag     <= 1'b0;
            overflow_flag    <= 1'b0;
            event_pulse      <= 1'b0;
            active_prev      <= 1'b0;
            width_count      <= 32'd0;
            period_count     <= 32'd0;
            have_previous    <= 1'b0;
        end else begin
            event_pulse <= 1'b0;
            active_prev <= active_now;

            if (clear_stats) begin
                event_count      <= 32'd0;
                last_width       <= 32'd0;
                last_period      <= 32'd0;
                too_narrow_count <= 32'd0;
                timeout_flag     <= 1'b0;
                overflow_flag    <= 1'b0;
                width_count      <= active_now ? 32'd1 : 32'd0;
                period_count     <= 32'd0;
                have_previous    <= 1'b0;
                active_prev      <= active_now;
            end else begin
                if (period_count != 32'hffffffff)
                    period_count <= period_count + 1'b1;
                else
                    overflow_flag <= 1'b1;

                if (monitor_enable && have_previous &&
                    (timeout_ticks != 0) && (period_count >= timeout_ticks))
                    timeout_flag <= 1'b1;

                if (active_now && !active_prev) begin
                    event_pulse  <= 1'b1;
                    width_count  <= 32'd1;
                    timeout_flag <= 1'b0;
                    if (event_count != 32'hffffffff)
                        event_count <= event_count + 1'b1;
                    else
                        overflow_flag <= 1'b1;

                    if (have_previous)
                        last_period <= period_count;
                    else
                        have_previous <= 1'b1;
                    period_count <= 32'd1;
                end else if (active_now) begin
                    if (width_count != 32'hffffffff)
                        width_count <= width_count + 1'b1;
                    else
                        overflow_flag <= 1'b1;
                end

                if (!active_now && active_prev) begin
                    last_width <= width_count;
                    if (width_count < 2) begin
                        if (too_narrow_count != 32'hffffffff)
                            too_narrow_count <= too_narrow_count + 1'b1;
                        else
                            overflow_flag <= 1'b1;
                    end
                    width_count <= 32'd0;
                end
            end
        end
    end
endmodule
