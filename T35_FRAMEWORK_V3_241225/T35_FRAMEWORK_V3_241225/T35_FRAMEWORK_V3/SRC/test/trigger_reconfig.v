`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/4/6
// Design Name: 
// Module Name: 
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: 
// Dependencies: 
// 
// Revision:
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module trigger_reconfig(
		
			input	wire   			clk  			,//clk25M
			input	wire			remote_reconfig	,
			input	wire	[1:0]   address_sel		,			
						
		  	output  wire	[1:0] 	cfg_CBSEL		,//
  		  	output 	reg 			cfg_CONFIG=0	,
  		  	output 	reg 			cfg_ENA=0		

);


assign cfg_CBSEL = address_sel;

reg [1:0]	reconfig_st_t =2'b00;


always @ (posedge clk)begin 
	reconfig_st_t<={reconfig_st_t[0],remote_reconfig};
end 

always @ (posedge clk )begin 
	cfg_ENA <= reconfig_st_t[0];
	cfg_CONFIG <= reconfig_st_t[1];
end 



endmodule