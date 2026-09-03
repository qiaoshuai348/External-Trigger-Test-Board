`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/4/6
// Design Name: ddrorbypass
// Module Name: ddrorbypass
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

module ddrorbypass (
			
				input	wire				sclk				,
		`ifdef FX3TXNUMTEST
				input	wire				rst				    ,
		`endif 
				input	wire				isddr				,	
				//input	wire				framest				,//test
				//input	wire				frameed				,//test	
				input   wire	[63:0]		diretly_data		,
				input	wire				diretly_vaild		,									
				input	wire	[63:0]		ddr_data			,
				input	wire				ddr_vld				,
			
		
				output	reg 	[65:0]		fx3_data			,
				output	reg					fx3_vaild =0			
				//output	reg		[31:0]		fx3txnum64=0		 //test
				

);


	reg [63:0]	 fx3_data_select =0 ;
	reg 		 fx3_vaild_select=0	;
	reg [63:0]   fx3_data_select_t0=0;
	reg 		 fx3_vaild_select_t0=0;
	reg 		 head_flga		 =0	;
	reg 		 end_flag		 =0	;
	

		
//****************************************************************************************


`ifdef FX3TESTDATA_Gen
	
always @(posedge sclk )begin
	
		fx3_vaild_select <= diretly_vaild;
		fx3_data_select  <= diretly_data;
	
end 
//diretly_data fx3_vaild_select

`else 
	
always @(posedge sclk )begin
	if(isddr)begin 
		fx3_vaild_select <= ddr_vld;
		fx3_data_select  <= ddr_data;
	end else begin 
		fx3_vaild_select <= diretly_vaild;
		fx3_data_select  <= diretly_data;
	end 
end 
//diretly_data fx3_vaild_select

`endif

always @(posedge sclk )begin
	if(fx3_data_select[47:0]==48'h4444EE11DD22&&fx3_vaild_select)begin 
			head_flga<=1;
	end else begin 
			head_flga<=0;
	end 
end 
//head_flga

always@(posedge sclk)begin 
	if(fx3_data_select[63:16]==48'hEE11DD226666&&fx3_vaild_select)begin
			end_flag<=1;
	end else begin 
			end_flag<=0;
	end 
end 
///

always@(posedge sclk)begin 
	
	fx3_data_select_t0<=fx3_data_select;
	fx3_vaild_select_t0<=fx3_vaild_select;
		
	fx3_data[31:0] <= fx3_data_select_t0[31:0];
	fx3_data[32]<=head_flga|end_flag;
	fx3_data[64:33]<=fx3_data_select_t0[63:32];
	fx3_data[65]<=head_flga|end_flag;
	
	fx3_vaild<= fx3_vaild_select_t0;
end 

//fx3_data
//fx3_vaild

`ifdef FX3TXNUMTEST

reg		[31:0]		Fx3TxPixNum64=0	;
reg 	[31:0] 		Fx3TxAllNum64=0 ;
reg     			rst_t0 			;
reg   				rst_t1 			;
reg  				rst_d  			;
reg  				f_val=0			;

always @ (posedge sclk)begin
	if(head_flga)begin
		f_val<=1;
	end else if(end_flag)begin
		f_val<=0;
	end else begin
		f_val<=f_val;
	end 
end 
//fval 

always @ (posedge sclk)begin
	rst_t0<=rst;
	rst_t1<=rst_t0;
	rst_d<=(rst_t1==0&&rst_t0==1);
end 
//rst_d
always @(posedge sclk )begin
	if(head_flga)begin 
		Fx3TxPixNum64 <=2;
	end else if(fx3_vaild_select&&f_val)begin
		Fx3TxPixNum64 <= Fx3TxPixNum64 + 1'b1 ;
	end else begin 
		Fx3TxPixNum64 <= Fx3TxPixNum64;
	end
end 
//fx3txnum64
always @(posedge sclk)begin
	if(rst_d)begin
		Fx3TxAllNum64<=0;
	end else if (fx3_vaild_select)begin
		Fx3TxAllNum64<=Fx3TxAllNum64+1;
	end else begin
		Fx3TxAllNum64<=Fx3TxAllNum64;
	end 
end 
//fx3txallnum64
`endif




	//wire [3:0]	wr_datacount_o  	;
	//wire		empty_o				;
	//reg			rd_en_i		=0		;
//always @ (posedge sclk)begin 
//	if(empty_o==0)begin 
//		rd_en_i <= 1'b1;
//	end else begin 
//		rd_en_i <= 1'b0;
//	end 
//end 
////rd_en_i
//
//
//always @ (posedge sclk)begin 
//	fx3_vld <= rd_en_i&(~empty_o);
//end 
////fx3_vld


//ddrorbypass_full
//ddrorbypass_fifo 		u_ddrorbypass_fifo(
//
//		.full_o 		( 		 			),
//		.empty_o 		( empty_o 			),
//		.rdata 			( fx3_data 			),
//		.wr_clk_i 		( sclk 				),
//		.rd_clk_i 		( sclk 				),
//		.wr_en_i		( fx3_vaild_select 	),
//		.rd_en_i		( rd_en_i 			),
//		.a_rst_i		( ~rstn				),
//		.wdata 			( fx3_data_select 	),
//		.wr_datacount_o (  					),//[3:0] wr_datacount_o
//		.rd_datacount_o ( 				 	),
//		.rst_busy 		(  					)
//		
//);


endmodule