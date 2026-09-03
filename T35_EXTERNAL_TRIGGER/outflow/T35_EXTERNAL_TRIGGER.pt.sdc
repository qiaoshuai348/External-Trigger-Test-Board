
# Efinity Interface Designer SDC
# Version: 2021.2.323.2.18
# Date: 2026-09-01 16:26

# Copyright (C) 2017 - 2021 Efinix Inc. All rights reserved.

# Device: T35F324
# Project: T35_EXTERNAL_TRIGGER
# Timing Model: I4 (final)

# PLL Constraints
#################
create_clock -period 10.00 syspll_CLKOUT100

# GPIO Constraints
####################
create_clock -period <USER_PERIOD> [get_ports {sysclk}]

# LVDS RX GPIO Constraints
############################
# set_input_delay -clock <CLOCK> -max <MAX CALCULATION> [get_ports {MUX_GPIO1_IN}]
# set_input_delay -clock <CLOCK> -min <MIN CALCULATION> [get_ports {MUX_GPIO1_IN}]
# set_input_delay -clock <CLOCK> -max <MAX CALCULATION> [get_ports {MUX_GPIO3_IN}]
# set_input_delay -clock <CLOCK> -min <MIN CALCULATION> [get_ports {MUX_GPIO3_IN}]
# set_output_delay -clock <CLOCK> -max <MAX CALCULATION> [get_ports {MUX_GPIO2_OUT}]
# set_output_delay -clock <CLOCK> -min <MIN CALCULATION> [get_ports {MUX_GPIO2_OUT}]
# set_output_delay -clock <CLOCK> -max <MAX CALCULATION> [get_ports {MUX_GPIO4_OUT}]
# set_output_delay -clock <CLOCK> -min <MIN CALCULATION> [get_ports {MUX_GPIO4_OUT}]

# LVDS Rx Constraints
####################
