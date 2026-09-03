`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/8/2
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: 
// Dependencies: 
// 
// Revision:T35V2  2022 07 11  T35PCBV2   
//				
// 
// 
//--------------------------------------------------------------------------------

module LED_state (

	input	wire			clk			,//25M clk  40ns 
	input	wire			rst			,
	input	wire	[7:0]	LED_control	,
	input	wire	[7:0]	subversion1	,
	input	wire	[7:0]	fpga_state1	,//fpga_state1[5] cfg_ERROR ;fpga_state1[6]MIPI Ecc no error;fpga_state1[1] check_fail
	input	wire	[7:0]	fpga_satte2	,
	input	wire	[7:0]	fpga_state3 ,
	
	output	reg 			LED_R=0		,//
	output	reg				LED_G=0		,
	output	reg 			LED_B=0	
			
);
//0.3s short led  ,0.7s long led 

reg [22:0] short_cnt	=0;//167ms
reg [26:0] long_cnt_s	=0;//2.684s    

initial begin  
	LED_R=0	;
	LED_G=0	;
	LED_B=0	;	
end 

always @ (posedge clk )begin 
	short_cnt <=short_cnt + 1 ; 
end       
//short_cnt

always @ (posedge clk )begin 
	if(long_cnt_s[26]==1)begin 
		long_cnt_s <=long_cnt_s;
	end else begin 
		long_cnt_s <=long_cnt_s+1;
	end 
end 
//long_cnt_s

always @ (posedge clk )begin 
	if(LED_control[7])begin 
		  LED_R<= LED_control[0];
	end else if(subversion1[7]&long_cnt_s[26]==0)begin//user image 
		  LED_R<=1;
	end else if( long_cnt_s[26]==0)begin //factory image 
		  LED_R<= short_cnt[22]; 
	end else begin
		  LED_R<=0;
	end 
end 
//LED_R

always @ (posedge clk )begin 
	if(LED_control[7])begin 
		LED_G<=LED_control[1];	
	//end else if(fpga_state1[6]==0) begin // pga_state1[6]MIPI Ecc no error
	//	LED_G<=short_cnt[22];	
	end else if(fpga_state1[5])begin //fpga_state1[5] cfg_ERROR
		LED_G<=1;//		
	end else begin 
		LED_G<=0;
	end 		
end 

//LED_G
		
always @ (posedge clk )begin 
	if(LED_control[7])begin 
		LED_B<=LED_control[2];
	end else if(fpga_state1[1]) begin //fpga_state1[1] ddr check_fail
		LED_B <= 1;
	end else begin 
		LED_B <=0;
	end 
end 
//LED_B
			
	


endmodule