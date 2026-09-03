`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/5/10
// Design Name: T35_FRAMEWORK
// Module Name: fx3_tx_new
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2 
// Dependencies: 1 解决 图像数据是帧头的时候数据误发数据头包。2 简化代码
// 
// Revision:rev2
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------




module fx3_tx_new(

	input 					out_clk				;
	input [63:0] 			out_data			;///*synthesis keep*/
	input 					out_data_valid		;// /*synthesis keep*/	
	input 					fx3_clk				;
	input 					fx3_rdy				;
	
	output reg 				fx3_slwr			=0;
	output reg[31:0] 		fx3_data			=0;
	output reg 				fx3_pkend			=0;
	output reg 				flag_full			=0;//  YSK modification

);

	wire 		fifo_empty		;
	reg 		fifo_rdreq		=0;
	wire[31:0] 	fifo_data		;
	reg 		fifo_data_valid	=0;
	reg	[12:0] 	fx3_buff_count	=0;
	reg	[2:0] 	sync_delay		=0;  
	wire		wr_full			;	
	
//***********************//YSK add *************************************************	
	wire[12:0]  wrusedw;//YSK add 
	
	always @(posedge out_clk )begin 
		if (  wrusedw > 400||wr_full==1)begin 
			  flag_full <= 1;
		end else begin 
			  flag_full <= 0;
		end 
	end 
	//   //YSK add 
//*********************TEST**********************************************************
//reg	[31:0]  test_num	/*synthesis keep*/	;
//reg [31:0]  test_cnt	/*synthesis keep*/	;

//always @(posedge out_clk )begin 
//	if (out_data ==32'hEE11DD22&&out_data_valid)begin 
//		test_num <= test_cnt ;
//	end else begin 
//		test_num <= test_num ;
//	end 
//end 
////test_num
//always @(posedge out_clk )begin 
//	if (out_data ==32'hEE11DD22&&out_data_valid)begin 
//		test_cnt <= 0;
//	end else if (out_data_valid )begin 
//		test_cnt <= test_cnt + 1;
//	end else begin 
//		test_cnt <=test_cnt ;
//	end 
//end 
//test_cnt

//	
//	reg [63:0]	data_t	;
//	reg			vld_t	;
//	
//	
//	always @ (posedge out_clk)begin 
//		data_t <= out_data;
//		vld_t  <= out_data_valid;
//	end 
	
fx3tx_fifo 	u_fx3tx_fifo(

		.full_o 		( wr_full 		 ),
		.empty_o	 	( fifo_empty 	 ),
		.rdata 			( fifo_data 	 ),
		.wr_clk_i 		( out_clk 		 ),
		.rd_clk_i 		( fx3_clk 		 ),
		.wr_en_i 		( out_data_valid ),//out_data_valid
		.rd_en_i 		( fifo_rdreq 	 ),
		.a_rst_i 		( 1'b0		 	 ),
		.wdata 			( out_data 		 ),//out_data
		.wr_datacount_o ( wrusedw 		 ),
		.rd_datacount_o ( 				 ),
		.rst_busy 		(  		 		 )
		
);


	initial
	begin
		fifo_rdreq <= 1'b0;
		fx3_slwr <= 1'b0;
		fifo_data_valid <= 1'b0;
		fx3_buff_count <= 16'd0;
		sync_delay <= 3'd0;
	end

	
	always @(posedge fx3_clk)
	begin
		if (fifo_empty || (!fx3_rdy) || (sync_delay != 3'd0))
		begin
			fifo_rdreq <= 1'b0;
		end
		else
		begin
			if (fifo_data_valid && (fifo_data == 32'hEE11DD22))
			begin
				fifo_rdreq <= 1'b0;
			end
			else
			begin
				if (fx3_buff_count < 16'd4093)
					fifo_rdreq <= 1'b1;
				else if ((fx3_buff_count == 16'd4093) && (fifo_rdreq + fifo_data_valid + fx3_slwr < 2'd3))
					fifo_rdreq <= 1'b1;
				else if ((fx3_buff_count == 16'd4094) && (fifo_rdreq + fifo_data_valid + fx3_slwr < 2'd2))
					fifo_rdreq <= 1'b1;
				else if ((fx3_buff_count == 16'd4095) && (fifo_rdreq + fifo_data_valid + fx3_slwr == 2'd0))
					fifo_rdreq <= 1'b1;
				else
					fifo_rdreq <= 1'b0;
			end
		end
	end
//




	always @(posedge fx3_clk)
	begin
		if ((!fifo_empty) && fifo_rdreq)
			fifo_data_valid <= 1'b1;
		else if (fx3_rdy && (sync_delay == 3'd0))
			fifo_data_valid <= 1'b0;
		else
			fifo_data_valid <= fifo_data_valid;
	end

	always @(posedge fx3_clk)
	begin
		if (fx3_rdy && (sync_delay == 3'd0))
		begin
			fx3_slwr <= fifo_data_valid;
			fx3_data[31:0] <= fifo_data[31:0];
		end
		else
		begin
			fx3_slwr <= 1'b0;
			fx3_data[31:0] <= fx3_data[31:0];
		end
	end

	always @(posedge fx3_clk)
	begin
		if ((!fx3_rdy) || (fx3_buff_count == 16'd4096))
			fx3_buff_count <= 16'd0;
		else if (fx3_slwr)
			fx3_buff_count <= fx3_buff_count + 1'b1;
		else
			fx3_buff_count <= fx3_buff_count;
	end

	always @(posedge fx3_clk)
	begin
		if (fx3_slwr && (fx3_buff_count == 16'd4095))
			sync_delay <= 3'd4;
		else if (fifo_data_valid && (fifo_data == 32'hEE11DD22) && fx3_rdy && (sync_delay == 3'd0))
			sync_delay <= 3'd5;
		else if (sync_delay > 3'd0)
			sync_delay <= sync_delay - 3'd1;
		else
			sync_delay <= 3'd0;
	end

	always @(posedge fx3_clk)
	begin
		if (fifo_data_valid && (fifo_data == 32'hEE11DD22) && fx3_rdy && (sync_delay == 3'd0))
			fx3_pkend <= 1'b1;
		else
			fx3_pkend <= 1'b0;
	end

endmodule
