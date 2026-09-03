`timescale 1ns/1ps

module reset_release #(
    parameter COUNT_BITS = 8
)(
    input  wire clk,
    input  wire pll_locked,
    output wire rst_n
);
    reg [COUNT_BITS-1:0] count;

    always @(posedge clk or negedge pll_locked) begin
        if (!pll_locked)
            count <= {COUNT_BITS{1'b0}};
        else if (!count[COUNT_BITS-1])
            count <= count + {{(COUNT_BITS-1){1'b0}}, 1'b1};
    end

    assign rst_n = pll_locked && count[COUNT_BITS-1];
endmodule
