`timescale 1ns/1ps

// 8N1 transmitter. A phase accumulator avoids the integer-divider baud error.
module uart_tx #(
    parameter CLK_HZ = 100000000,
    parameter BAUD   = 115200
)(
    input  wire       clk,
    input  wire       rst_n,
    input  wire [7:0] data_in,
    input  wire       data_valid,
    output wire       data_ready,
    output reg        tx,
    output reg        busy
);
    reg [31:0] phase;
    reg [9:0]  frame;
    reg [3:0]  bit_index;
    wire [32:0] phase_sum = {1'b0, phase} + BAUD;
    wire [32:0] phase_remainder = phase_sum - CLK_HZ;
    wire baud_tick = busy && (phase_sum >= CLK_HZ);

    assign data_ready = !busy;

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            phase     <= 32'd0;
            frame     <= 10'h3ff;
            bit_index <= 4'd0;
            tx        <= 1'b1;
            busy      <= 1'b0;
        end else if (!busy) begin
            phase <= 32'd0;
            tx    <= 1'b1;
            if (data_valid) begin
                frame     <= {1'b1, data_in, 1'b0};
                bit_index <= 4'd0;
                tx        <= 1'b0;
                busy      <= 1'b1;
            end
        end else if (baud_tick) begin
            phase <= phase_remainder[31:0];
            if (bit_index == 4'd9) begin
                tx   <= 1'b1;
                busy <= 1'b0;
            end else begin
                bit_index <= bit_index + 1'b1;
                tx        <= frame[bit_index + 1'b1];
            end
        end else begin
            phase <= phase + BAUD;
        end
    end
endmodule
