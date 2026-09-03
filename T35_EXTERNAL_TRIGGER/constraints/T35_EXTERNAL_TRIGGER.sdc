# 50 MHz board oscillator and 100 MHz PLL output.
create_clock -period 20.000 -name sysclk_in [get_ports {sysclk}]
create_clock -period 10.000 -name syspll_CLKOUT100 [get_ports {syspll_CLKOUT100}]

# Only the asynchronous pad-to-first-stage arcs are cut. The first-to-second
# synchronizer stages remain timed by the normal 100 MHz setup/hold analysis.
set_false_path -from [get_ports {MUX_GPIO1_IN}] -to [get_cells {*gpio1_sync_ff1*}]
set_false_path -from [get_ports {MUX_GPIO3_IN}] -to [get_cells {*rx_sync_ff1*}]
