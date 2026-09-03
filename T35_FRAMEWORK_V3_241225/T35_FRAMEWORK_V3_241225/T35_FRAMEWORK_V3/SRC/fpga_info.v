`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/5/5
// Design Name: T35_TOP
// Module Name: fpga_info
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: 
// Dependencies: 
// 
// Revision:rev1 22 05 05 
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module fpga_info(

	output	 wire	[7:0]	year	,
	output	 wire	[7:0]	month	,
	output	 wire	[7:0]	day		,
	output	 wire	[7:0]	subversion1,
	output	 wire	[7:0]	subversion2,
	output	 wire	[7:0]	boardty 
	
);

assign  year  		= 8'd24,
		month 		= 8'd12,
		day	  		= 8'd25,
		subversion1	= 8'h00, 
		subversion2	= 8'd20,//20
		boardty 	= 8'd04;


endmodule 