`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/6/9
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: 
// Dependencies: 
// 
// Revision:rev1
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module Trigin_set(

		input	wire			clk					, //25M  40NS
		input	wire			IDLE				,
		//input	wire			SingleFrameCapture	,
		input	wire [7:0]		test_mode			, //reg39 gpio trig enable 
		input	wire [7:0]		TrigMode    		, //reg58 Master switch,enable trigout 
		input	wire [7:0]		TrigModeA   		,//TrigModeA   reg158
		
		input	wire			trigin_gpio			, //gpio trig in	
		input	wire [23:0]		FilterTime		    , // in this mode(mode2), FilterTime Determines the interval between IDLE  and RELEASEIDLE  FilterTime value*40ns == xx ns 25000 000 ;
		input   wire			trigin_optic		, //optic trig in 锛宎ctive low ,It is usually high level
		
		output	reg  			trigin_or_idle=0
		
);

reg		[23:0]  interval_cnt	= 0;
reg				trig_in_flag	= 0;
reg				trig_in_T1		= 0;
reg				trig_in_T2      = 0;
reg				trig_in_T3		= 0; 
reg				trig_in			= 0;
reg				trigin_filter	= 0;

wire			posedge_plus	  ;
wire			negedge_plus	  ;


always @(posedge clk )begin 
	if(TrigMode==1||TrigMode==3)begin//enable trig in
		if (test_mode==2) 
			trig_in <= trigin_gpio;
		else 
			trig_in <= ~trigin_optic;
    end else begin 
    	trig_in<= 0;
    end 
	
end 


always @ (posedge clk )begin 
	trig_in_T1 <= trig_in ;
	trig_in_T2 <= trig_in_T1;
	trig_in_T3 <= trig_in_T2;
end 


//assign negedge_plus = (~trig_in_T2)&trig_in_T3;
assign posedge_plus = (~trig_in_T3)&trig_in_T2;

always @ (posedge clk )begin 
    if ((interval_cnt > FilterTime) ||TrigMode==0||TrigMode==2)begin 
		trig_in_flag <= 0;
	end else if ( posedge_plus ==1)begin //TrigMode_4 ==0 &&
		trig_in_flag <= 1;
	end else begin 
		trig_in_flag <= trig_in_flag;
	end 
end 
//trin_in2_flag
always @ (posedge clk )begin 
   if (trig_in_flag)begin 
		interval_cnt <= interval_cnt + 1;
	end else begin 
		interval_cnt <= 0;
	end 
end 
//interval_cnt

always @ (posedge clk )begin 
	if(interval_cnt==(FilterTime-2)&&trig_in_T3==1'b1)begin
		trigin_filter<=1'b1;
	end else begin
		trigin_filter<=1'b0;
	end 
end 
//trigin_filter


always @ (posedge clk )begin 
	if(TrigMode==1||TrigMode==3)begin//enable trig in
		trigin_or_idle <=trigin_filter;	
//	 end else if(TrigModeA[2]==1'b1)begin                   //2023 07 04 take out SingleFrameCapture
//		trigin_or_idle<= SingleFrameCapture;
    end else begin 
    	trigin_or_idle<= IDLE;
    end 
end 

endmodule