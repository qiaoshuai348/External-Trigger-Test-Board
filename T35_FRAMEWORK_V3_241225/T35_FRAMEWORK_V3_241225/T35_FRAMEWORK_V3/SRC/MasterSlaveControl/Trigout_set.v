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
module Trigout_set(

		input	wire		clk					,//25mhz
		input	wire [7:0]	TrigMode 			,//reg58 Master switch,enable trigout 
		input	wire [1:0]	TrigModeA		    ,//TrigModeA   reg153
		input	wire		trigout_slave		,  
		input	wire		trigout_xtrig		,
		input	wire		trigin				,
		
		output	wire		trigout_gpio		,
		output	wire		trigout_optic			

);


reg [11:0] cnt_us     =0;
reg trigout_mux		  =0;
reg trigout_long      =0;

always@(posedge clk)begin
	if(TrigMode==2||TrigMode==3)begin //   TrigMode==2||TrigMode==3
		trigout_mux<=0;
	end else begin
		case(TrigModeA[1:0])//reg153
		0:   trigout_mux <= trigout_xtrig ;//xtrig
		1:	 trigout_mux <= trigin		  ;//trig in
		2:	 trigout_mux <= trigout_slave ;//slave control trig out 	
		default: trigout_mux <=  trigout_slave ;
		endcase
	end 
end 
//trigout_mux
			
	

always @ (posedge clk)begin 
	if(trigout_long)begin 
		cnt_us <= cnt_us+1'b1;
	end else begin 
		cnt_us <=0;
	end 
end 
//cnt_us
always @ (posedge clk)begin 
	if(trigout_mux)begin 
		trigout_long <= 1;
	end else if((cnt_us[11]==1&&cnt_us[9]==1)||(TrigMode[1]==1'b0))begin //about 102us
		trigout_long <= 0;
	end else begin 
		trigout_long <= trigout_long;
	end 
end 
//trigout_long
                    
assign   trigout_gpio  =   trigout_long;
assign   trigout_optic =   trigout_long;  
                 
endmodule