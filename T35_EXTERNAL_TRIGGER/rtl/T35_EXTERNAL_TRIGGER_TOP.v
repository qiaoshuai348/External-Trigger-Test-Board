`timescale 1ns/1ps

module T35_EXTERNAL_TRIGGER_TOP(
    input  wire sysclk,
    input  wire syspll_CLKOUT100,
    input  wire syspll_LOCKED,
    input  wire MUX_GPIO1_IN,
    output wire MUX_GPIO2_OUT,
    input  wire MUX_GPIO3_IN,
    output wire MUX_GPIO4_OUT
);
    wire core_rst_n;
    wire trigger_out_internal;
    wire uart_tx_internal;

    reset_release #(.COUNT_BITS(8)) reset_release_inst (
        .clk(syspll_CLKOUT100), .pll_locked(syspll_LOCKED), .rst_n(core_rst_n)
    );

    external_trigger_controller #(
        .CLK_HZ(100000000), .BAUD(115200)
    ) controller_inst (
        .clk(syspll_CLKOUT100),
        .rst_n(core_rst_n),
        .uart_rx_pin(MUX_GPIO3_IN),
        .uart_tx_pin(uart_tx_internal),
        .trigger_in_pin(MUX_GPIO1_IN),
        .trigger_out_pin(trigger_out_internal)
    );

    // Explicit safe levels do not depend on a running state machine.
    assign MUX_GPIO2_OUT = core_rst_n ? trigger_out_internal : 1'b0;
    assign MUX_GPIO4_OUT = core_rst_n ? uart_tx_internal : 1'b1;

    // The physical sysclk pad is consumed by the Interface Designer PLL. The
    // generated interface also exposes it to the core, where no fabric use is required.
    wire unused_sysclk = sysclk;
endmodule
