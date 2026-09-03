`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/4/6
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: 
// Dependencies: 
// 
// Revision:rev2
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module cmos_ctrl(
	
		input	wire		cmos_clk		,
		input	wire		rstn			,
		
		output	wire		cmosctl_xhs		,
		output	wire		cmosctl_xvs		,
		output	wire		cmosctl_ampv	,
		output	wire		cmosctl_xclr	,//REG 00
		output	wire		cmosctl_slasel	


);

assign cmosctl_ampv = 1'B0;




endmodule 