`timescale 1ns/1ps

// 8N1 receiver with a 16x fractional oversampling clock.
module uart_rx #(
    parameter CLK_HZ = 100000000,
    parameter BAUD   = 115200
)(
    input  wire       clk,
    input  wire       rst_n,
    input  wire       rx,
    output reg  [7:0] data_out,
    output reg        data_valid,
    output reg        frame_error
);
    localparam OS_RATE = BAUD * 16;
    localparam S_IDLE  = 2'd0;
    localparam S_START = 2'd1;
    localparam S_DATA  = 2'd2;
    localparam S_STOP  = 2'd3;

    (* async_reg = "true" *) reg rx_sync_ff1;
    (* async_reg = "true" *) reg rx_sync_ff2;
    reg        rx_prev;
    reg [1:0]  state;
    reg [31:0] phase;
    reg [3:0]  os_count;
    reg [2:0]  bit_index;
    reg [7:0]  shift;
    wire [32:0] phase_sum = {1'b0, phase} + OS_RATE;
    wire [32:0] phase_remainder = phase_sum - CLK_HZ;
    wire os_tick = (state != S_IDLE) && (phase_sum >= CLK_HZ);

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            rx_sync_ff1 <= 1'b1;
            rx_sync_ff2 <= 1'b1;
            rx_prev     <= 1'b1;
        end else begin
            rx_sync_ff1 <= rx;
            rx_sync_ff2 <= rx_sync_ff1;
            rx_prev     <= rx_sync_ff2;
        end
    end

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            data_out   <= 8'd0;
            data_valid <= 1'b0;
            frame_error<= 1'b0;
            state      <= S_IDLE;
            phase      <= 32'd0;
            os_count   <= 4'd0;
            bit_index  <= 3'd0;
            shift      <= 8'd0;
        end else begin
            data_valid  <= 1'b0;
            frame_error <= 1'b0;

            if (state == S_IDLE) begin
                phase    <= 32'd0;
                os_count <= 4'd0;
                if (rx_prev && !rx_sync_ff2) begin
                    state <= S_START;
                    phase <= 32'd0;
                end
            end else if (os_tick) begin
                phase <= phase_remainder[31:0];
                case (state)
                    S_START: begin
                        if (os_count == 4'd7) begin
                            os_count <= 4'd0;
                            if (!rx_sync_ff2) begin
                                state     <= S_DATA;
                                bit_index <= 3'd0;
                            end else begin
                                state <= S_IDLE;
                            end
                        end else begin
                            os_count <= os_count + 1'b1;
                        end
                    end
                    S_DATA: begin
                        if (os_count == 4'd15) begin
                            shift[bit_index] <= rx_sync_ff2;
                            os_count <= 4'd0;
                            if (bit_index == 3'd7)
                                state <= S_STOP;
                            else
                                bit_index <= bit_index + 1'b1;
                        end else begin
                            os_count <= os_count + 1'b1;
                        end
                    end
                    S_STOP: begin
                        if (os_count == 4'd15) begin
                            data_out <= shift;
                            if (rx_sync_ff2)
                                data_valid <= 1'b1;
                            else
                                frame_error <= 1'b1;
                            state    <= S_IDLE;
                            os_count <= 4'd0;
                        end else begin
                            os_count <= os_count + 1'b1;
                        end
                    end
                    default: state <= S_IDLE;
                endcase
            end else begin
                phase <= phase + OS_RATE;
            end
        end
    end
endmodule
