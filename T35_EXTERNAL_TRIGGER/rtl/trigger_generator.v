`timescale 1ns/1ps

module trigger_generator(
    input  wire        clk,
    input  wire        rst_n,
    input  wire        cfg_apply,
    input  wire [31:0] cfg_period,
    input  wire [31:0] cfg_width,
    input  wire        cfg_active_low,
    input  wire        start,
    input  wire [31:0] start_count,
    input  wire        stop,
    output wire        trigger_out,
    output reg         running,
    output reg         precharge,
    output reg         pending_update,
    output reg  [31:0] active_period,
    output reg  [31:0] active_width,
    output reg         active_low,
    output reg  [31:0] remaining,
    output reg         cycle_boundary
);
    reg [31:0] phase;
    reg [31:0] target_count;
    reg [31:0] emitted_count;
    reg [31:0] pending_period;
    reg [31:0] pending_width;
    reg        pending_active_low;

    wire pulse_level = active_low ? 1'b0 : 1'b1;
    wire idle_level  = active_low ? 1'b1 : 1'b0;
    assign trigger_out = !running ? 1'b0 :
                         precharge ? 1'b1 :
                         ((phase < active_width) ? pulse_level : idle_level);

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            running           <= 1'b0;
            precharge         <= 1'b0;
            pending_update    <= 1'b0;
            active_period     <= 32'd100000;
            active_width      <= 32'd20;
            active_low        <= 1'b0;
            remaining         <= 32'd0;
            phase             <= 32'd0;
            target_count      <= 32'd0;
            emitted_count     <= 32'd0;
            pending_period    <= 32'd0;
            pending_width     <= 32'd0;
            pending_active_low<= 1'b0;
            cycle_boundary    <= 1'b0;
        end else begin
            cycle_boundary <= 1'b0;

            if (cfg_apply) begin
                if (!running) begin
                    active_period <= cfg_period;
                    active_width  <= cfg_width;
                    active_low    <= cfg_active_low;
                    pending_update<= 1'b0;
                end else begin
                    pending_period     <= cfg_period;
                    pending_width      <= cfg_width;
                    pending_active_low <= cfg_active_low;
                    pending_update     <= 1'b1;
                end
            end

            if (stop) begin
                running       <= 1'b0;
                precharge     <= 1'b0;
                phase         <= 32'd0;
                remaining     <= 32'd0;
                emitted_count <= 32'd0;
            end else if (start && !running) begin
                running       <= 1'b1;
                precharge     <= active_low;
                phase         <= 32'd0;
                target_count  <= start_count;
                emitted_count <= 32'd0;
                remaining     <= start_count;
            end else if (running) begin
                if (phase == active_period - 1'b1) begin
                    phase          <= 32'd0;
                    cycle_boundary <= 1'b1;
                    if (precharge) begin
                        precharge <= 1'b0;
                    end else begin
                        emitted_count <= emitted_count + 1'b1;
                        if (target_count != 0)
                            remaining <= target_count - emitted_count - 1'b1;

                        if ((target_count != 0) && (emitted_count + 1'b1 >= target_count)) begin
                            running   <= 1'b0;
                            remaining <= 32'd0;
                        end
                    end

                    if (pending_update) begin
                        active_period  <= pending_period;
                        active_width   <= pending_width;
                        active_low     <= pending_active_low;
                        pending_update <= 1'b0;
                    end
                end else begin
                    phase <= phase + 1'b1;
                end
            end
        end
    end
endmodule
