`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/4/7
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

module fx3testdata(
		
		input	wire		clk			,
		input	wire		flag_full	,
		
		output	reg 		datavld=0	,
		output	wire [63:0]	data	
		
);
reg	[15:0]	cnt	=0	;//0-65535
wire[15:0]  cnt2;
assign cnt2 =cnt+1;

always @ (posedge clk )begin 
	if(flag_full==0)begin 
		datavld <= 1;
	end else begin 
		datavld <= 0;
	end 
end 
//cnt 

always @ (posedge clk )begin 
	if(cnt[15]==1)begin
		cnt <= 0;
	end else if(datavld==1)begin 
		cnt <= cnt + 1 ;
	end else begin 
		cnt <= cnt ;
	end 
end 


assign data = {cnt,cnt,cnt2,cnt2} ;

endmodule