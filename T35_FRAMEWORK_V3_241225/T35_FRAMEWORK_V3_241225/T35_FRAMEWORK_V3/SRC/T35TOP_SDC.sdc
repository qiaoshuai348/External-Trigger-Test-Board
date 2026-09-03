
##-------------------------------------------------------------------------------------------------------------------------------------------------
## Company:  QHYCCD
## Engineer: YangSK
## 
## Create Date: 2022/4/6 
## Design Name: T35_TOP
## Module Name: T35_TOP
## Project Name: T35_TOP
## Target Devices: t35f324
## Tool Versions: EFINITY21.2
## Description: 
## Dependencies: 
## 
## Revision:rev2
## 
## Additional Comments:
## 
##--------------------------------------------------------------------------------
# clock  &  PLL Constraints
#################
create_clock -period 20.00 -name sysclk_in [get_ports sysclk]
create_clock -period 10.00 -name ddrrefclk [get_ports ddrrefclk]


create_clock -period 10.00 syspll_CLKOUT100
create_clock -period 40.00 syspll_CLKOUT25
create_clock -period 166.67 CMOSPLL_CMOSCLKOUT

#ddr 800M
create_clock -period 2.50 ddrpll_CLKOUT 

#ddr 1066M
#create_clock -period 1.88 ddrpll_CLKOUT


set_multicycle_path 4 -setup -from [get_clocks syspll_CLKOUT100] -to [get_clocks syspll_CLKOUT25] -start
set_multicycle_path 3 -hold -from [get_clocks syspll_CLKOUT100] -to [get_clocks syspll_CLKOUT25] -start
set_multicycle_path 4 -setup -from [get_clocks syspll_CLKOUT25] -to [get_clocks syspll_CLKOUT100] -end 
set_multicycle_path 3 -hold -from [get_clocks syspll_CLKOUT25] -to [get_clocks syspll_CLKOUT100] -end 

set_clock_groups -asynchronous -group {CMOSPLL_CMOSCLKOUT} 
#set_clock_groups -asynchronous -group {syspll_CLKOUT25} -group {syspll_CLKOUT100}

# GPIO Constraints
####################
# set_input_delay -clock <CLOCK> -max <MAX CALCULATION> [get_ports {ddrrefclk}]
# set_input_delay -clock <CLOCK> -min <MIN CALCULATION> [get_ports {ddrrefclk}]
set_input_delay -clock syspll_CLKOUT25 -max 5.168 [get_ports {miso}]
set_input_delay -clock syspll_CLKOUT25 -min 2.584 [get_ports {miso}]
#create_clock -period <USER_PERIOD> [get_ports {sysclk}]
set_output_delay -clock syspll_CLKOUT25 -max -3.700 [get_ports {CS0}]
set_output_delay -clock syspll_CLKOUT25 -min -2.071 [get_ports {CS0}]
set_output_delay -clock syspll_CLKOUT25 -max -3.700 [get_ports {HOLD_N}]
set_output_delay -clock syspll_CLKOUT25 -min -2.071 [get_ports {HOLD_N}]
set_output_delay -clock syspll_CLKOUT25 -max -3.700 [get_ports {sclk}]
set_output_delay -clock syspll_CLKOUT25 -min -2.071 [get_ports {sclk}]
set_output_delay -clock syspll_CLKOUT25 -max -3.700 [get_ports {WP_N}]
set_output_delay -clock syspll_CLKOUT25 -min -2.071 [get_ports {WP_N}]
set_output_delay -clock syspll_CLKOUT25 -max -3.700 [get_ports {mosi_OUT}]
set_output_delay -clock syspll_CLKOUT25 -min -2.071 [get_ports {mosi_OUT}]
# set_output_delay -clock <CLOCK> -max <MAX CALCULATION> [get_ports {mosi_OE}]
# set_output_delay -clock <CLOCK> -min <MIN CALCULATION> [get_ports {mosi_OE}]

# MIPI RX Constraints
#####################################
set_output_delay -clock syspll_CLKOUT100 -max -3.746 [get_ports {mipi_rx_inst1_VC_ENA[3] mipi_rx_inst1_VC_ENA[2] mipi_rx_inst1_VC_ENA[1] mipi_rx_inst1_VC_ENA[0]}]
set_output_delay -clock syspll_CLKOUT100 -min -2.087 [get_ports {mipi_rx_inst1_VC_ENA[3] mipi_rx_inst1_VC_ENA[2] mipi_rx_inst1_VC_ENA[1] mipi_rx_inst1_VC_ENA[0]}]
set_output_delay -clock syspll_CLKOUT100 -max -4.197 [get_ports {mipi_rx_inst1_CLEAR}]
set_output_delay -clock syspll_CLKOUT100 -min -1.998 [get_ports {mipi_rx_inst1_CLEAR}]
set_input_delay -clock syspll_CLKOUT100 -max 5.394 [get_ports {mipi_rx_inst1_VSYNC[3] mipi_rx_inst1_VSYNC[2] mipi_rx_inst1_VSYNC[1] mipi_rx_inst1_VSYNC[0]}]
set_input_delay -clock syspll_CLKOUT100 -min 2.697 [get_ports {mipi_rx_inst1_VSYNC[3] mipi_rx_inst1_VSYNC[2] mipi_rx_inst1_VSYNC[1] mipi_rx_inst1_VSYNC[0]}]
set_input_delay -clock syspll_CLKOUT100 -max 5.388 [get_ports {mipi_rx_inst1_HSYNC[3] mipi_rx_inst1_HSYNC[2] mipi_rx_inst1_HSYNC[1] mipi_rx_inst1_HSYNC[0]}]
set_input_delay -clock syspll_CLKOUT100 -min 2.694 [get_ports {mipi_rx_inst1_HSYNC[3] mipi_rx_inst1_HSYNC[2] mipi_rx_inst1_HSYNC[1] mipi_rx_inst1_HSYNC[0]}]
set_input_delay -clock syspll_CLKOUT100 -max 5.242 [get_ports {mipi_rx_inst1_VALID}]
set_input_delay -clock syspll_CLKOUT100 -min 2.621 [get_ports {mipi_rx_inst1_VALID}]
set_input_delay -clock syspll_CLKOUT100 -max 5.312 [get_ports {mipi_rx_inst1_CNT[3] mipi_rx_inst1_CNT[2] mipi_rx_inst1_CNT[1] mipi_rx_inst1_CNT[0]}]
set_input_delay -clock syspll_CLKOUT100 -min 2.656 [get_ports {mipi_rx_inst1_CNT[3] mipi_rx_inst1_CNT[2] mipi_rx_inst1_CNT[1] mipi_rx_inst1_CNT[0]}]
set_input_delay -clock syspll_CLKOUT100 -max 5.340 [get_ports {mipi_rx_inst1_DATA[*]}]
set_input_delay -clock syspll_CLKOUT100 -min 2.670 [get_ports {mipi_rx_inst1_DATA[*]}]
set_input_delay -clock syspll_CLKOUT100 -max 5.257 [get_ports {mipi_rx_inst1_ERROR[*]}]
set_input_delay -clock syspll_CLKOUT100 -min 2.628 [get_ports {mipi_rx_inst1_ERROR[*]}]
set_input_delay -clock syspll_CLKOUT100 -max 5.255 [get_ports {mipi_rx_inst1_ULPS_CLK}]
set_input_delay -clock syspll_CLKOUT100 -min 2.627 [get_ports {mipi_rx_inst1_ULPS_CLK}]
set_input_delay -clock syspll_CLKOUT100 -max 5.264 [get_ports {mipi_rx_inst1_ULPS[3] mipi_rx_inst1_ULPS[2] mipi_rx_inst1_ULPS[1] mipi_rx_inst1_ULPS[0]}]
set_input_delay -clock syspll_CLKOUT100 -min 2.632 [get_ports {mipi_rx_inst1_ULPS[3] mipi_rx_inst1_ULPS[2] mipi_rx_inst1_ULPS[1] mipi_rx_inst1_ULPS[0]}]

# Configuration Control Constraints
#####################################
set_output_delay -clock syspll_CLKOUT25 -max -3.360 [get_ports {cfg_CBSEL[0]}]
set_output_delay -clock syspll_CLKOUT25 -max -3.360 [get_ports {cfg_CBSEL[1]}]
set_output_delay -clock syspll_CLKOUT25 -min -2.155 [get_ports {cfg_CBSEL[0]}]
set_output_delay -clock syspll_CLKOUT25 -min -2.155 [get_ports {cfg_CBSEL[1]}]
set_output_delay -clock syspll_CLKOUT25 -max -3.410 [get_ports {cfg_ENA}]
set_output_delay -clock syspll_CLKOUT25 -min -2.155 [get_ports {cfg_ENA}]

# DDR Constraints
#####################
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_AADDR_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_AADDR_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_ABURST_0[1] DDR_CTRL_ABURST_0[0]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_ABURST_0[1] DDR_CTRL_ABURST_0[0]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_AID_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_AID_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_ALEN_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_ALEN_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_ALOCK_0[1] DDR_CTRL_ALOCK_0[0]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_ALOCK_0[1] DDR_CTRL_ALOCK_0[0]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_ASIZE_0[2] DDR_CTRL_ASIZE_0[1] DDR_CTRL_ASIZE_0[0]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_ASIZE_0[2] DDR_CTRL_ASIZE_0[1] DDR_CTRL_ASIZE_0[0]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_ATYPE_0}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_ATYPE_0}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_AVALID_0}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_AVALID_0}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_BREADY_0}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_BREADY_0}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_RREADY_0}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_RREADY_0}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_WDATA_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_WDATA_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_WID_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_WID_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_WLAST_0}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_WLAST_0}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_WSTRB_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_WSTRB_0[*]}]
set_output_delay -clock syspll_CLKOUT100 -max -1.810 [get_ports {DDR_CTRL_WVALID_0}]
set_output_delay -clock syspll_CLKOUT100 -min -1.655 [get_ports {DDR_CTRL_WVALID_0}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_AREADY_0}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_AREADY_0}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_BID_0[*]}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_BID_0[*]}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_BVALID_0}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_BVALID_0}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_RDATA_0[*]}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_RDATA_0[*]}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_RID_0[*]}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_RID_0[*]}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_RLAST_0}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_RLAST_0}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_RRESP_0[1] DDR_CTRL_RRESP_0[0]}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_RRESP_0[1] DDR_CTRL_RRESP_0[0]}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_RVALID_0}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_RVALID_0}]
set_input_delay -clock syspll_CLKOUT100 -max 7.310 [get_ports {DDR_CTRL_WREADY_0}]
set_input_delay -clock syspll_CLKOUT100 -min 3.655 [get_ports {DDR_CTRL_WREADY_0}]


# LVDS RX GPIO Constraints
############################

# LVDS Rx Constraints
####################



# LVDS TX GPIO Constraints
############################

 
# set_output_delay -clock syspll_CLKOUT100 -max 1 [get_ports {fx3_ctl11}]
# set_output_delay -clock syspll_CLKOUT100 -max 1 [get_ports {fx3_ctl12}] 
# set_output_delay -clock syspll_CLKOUT100 -max 1 [get_ports {fx3_data[*]}]
 
 set_output_delay -clock syspll_CLKOUT100  1 [get_ports {fx3_ctl11}]
 set_output_delay -clock syspll_CLKOUT100  1 [get_ports {fx3_ctl12}] 
 set_output_delay -clock syspll_CLKOUT100  1 [get_ports {fx3_data[*]}]
 

 
#
# LVDS Tx Constraints
####################