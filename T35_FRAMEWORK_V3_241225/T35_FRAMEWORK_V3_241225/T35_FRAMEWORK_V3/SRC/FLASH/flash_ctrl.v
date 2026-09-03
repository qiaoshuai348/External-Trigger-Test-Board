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
// Description: 所有操作需要先设置写保护关闭:asmi_wpen=0；然后片选选择对应的FLASH
//对于读操作 先输入地址，读的byte数量，片选，然后拉高asmi_readstart，上升沿有效，
//对于写操作，1 先输入地址，写的byte数量，片选；2 拉高asmi_writestart，过4个时钟延迟，再输入有效数据，上升沿有效，
//对于擦除操作：先输入地址，然后拉高asmi_erasestart，上升沿有效，
//flash_cs :0 无操作；1 FPGA BIT FLASH CS0 ；2 FPGA parameter FLASH CS1  
//asmi_num ：比如写256个byte，这个参数需要设置成255.
// Dependencies: 
// 
// Revision:rev3
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module flash_ctrl(

			input	wire   			clk  			,//clk25M
			input	wire			rst				,//active high   
			input	wire	[1:0]	flash_cs		,// 
			input	wire	[23:0]	asmi_address	,
			input	wire	[7:0]	asmi_num		,//wite or read data bytes number
			input	wire			asmi_writestart	,//write start 
			input	wire			asmi_readstart	,//read start
			input	wire			asmi_erasestart	,//erase start 
			input	wire	[7:0]	asmi_didata		,
			input	wire			asmi_divld		,
			input	wire			asmi_wpen		,
			
			output	wire	[7:0]	asmi_dataout	,
			output	wire			asmi_dovalid	,
			output	wire			busy			,//修改为状态寄存器
			
//SPI ports  		
			input 	wire			miso 			,	
			//input 	wire			miso_1 		    ,
			//input 	wire			miso_2 		    ,
			//input 	wire			miso_3 		    ,
			                
			output  wire 			sclk 			,
			output  wire 			CS0 			,//fpga bit flash 
			output	wire			CS1				,//fpga parameter flash 
			output  wire 			mosi 			,
			output  reg 			HOLD_N	=0		,       
 			output  reg				WP_N	=0		,       

			//output  wire 			mosi_1 		    ,
			//output  wire 			mosi_2 		    ,
			//output  wire 			mosi_3 		    ,
			output  wire 			mosi_oe 		,
			//output  wire			mosi_oe1		, 
 			//output  wire			mosi_oe2		, 
 			//output  wire			mosi_oe3		,   
						
//reconfigure 			
			input	wire			remote_reconfig	,
			input	wire	[1:0]   address_sel		,			
						
		  	output  wire	[1:0] 	cfg_CBSEL		,//
  		  	output 	reg 			cfg_CONFIG=0	,
  		  	output 	reg 			cfg_ENA=0		
);


reg 		quad_enable			=0	;
reg 		quad_fast_read		=0	;
reg 		quad_page_write		=0	;

reg 		fast_read			=0	;
reg 		page_write			=0	;
reg 		sector_erase		=0	;
reg 		rden				=0	;
reg 		wren				=0	;
reg 		shift_bytes			=0	;
reg	[7:0]	data_in				=0	;

wire		nss						;
reg	[1:0]	asmi_readstart_t	=2'b00;
reg 		read_start_flag		=0	;
reg [1:0]	asmi_writestart_t	=2'b00;
//reg 		write_start_flag	=0	;
reg [1:0]	asmi_erasestart_t	=2'b00;

reg [1:0]	reconfig_st_t =2'b00;

//assign mosi_oe1 = mosi_oe;
//assign mosi_oe2 = mosi_oe;
//assign mosi_oe3 = mosi_oe;


reg [8:0] rd_cnt	=0;
reg [8:0] wr_cnt	=0;

always@(posedge clk)begin 
	HOLD_N<=~asmi_wpen;
	WP_N <=~asmi_wpen;
end 


assign CS0 = (flash_cs==1)?nss:1'b1;
assign CS1 = (flash_cs==2)?nss:1'b1;

//assign CS0 = nss;

///*******************jump start ************************************************** 
assign cfg_CBSEL = address_sel;


always @ (posedge clk)begin 
	reconfig_st_t<={reconfig_st_t[0],remote_reconfig};
end 

always @ (posedge clk )begin 
	cfg_ENA <= reconfig_st_t[0];
	cfg_CONFIG <= reconfig_st_t[1];
end 


//read flash **********************************************************************
//asmi_readstart_t
//always @ (posedge clk )begin 
//	if(rst)begin
//		quad_enable<=1'b0;
//	end else if (read_start_flag)begin 
//		quad_enable<=1'b1;
//	end else if(busy==1'b0)begin 
//		quad_enable<=1'b0;
//	end else begin 
//		quad_enable<=quad_enable;
//	end 
//end 
////quad_enable
		
//always @ (posedge clk )begin 
//	if(rst)begin
//		quad_fast_read<=1'b0;
//	end else if(quad_enable==1'b1&busy==1'b0)begin 
//		quad_fast_read<=1'b1;
//	end else if(busy==1'b0)begin 
//		quad_fast_read<=1'b0;
//	end else begin 
//		quad_fast_read<=quad_fast_read;
//	end 
//end 
////quad_fast_read
//always @ (posedge clk )begin 
//	if(rst)begin
//		rden<=1'b0;
//	end else if(quad_enable==1'b1&busy==1'b0)begin 
//		rden<=1'b1;
//	end else if(asmi_dovalid)begin 
//		rden<=1'b0;
//	end else begin 
//		rden<=rden;
//	end 
//end 
////rden

always @ (posedge clk )begin 
	asmi_readstart_t <={asmi_readstart_t[0],asmi_readstart};
	read_start_flag  <=(asmi_readstart_t[0])&(!asmi_readstart_t[1]);
end 

always @ (posedge clk )begin 
	if(read_start_flag)begin 
		rd_cnt <= 0;
	end else if(asmi_dovalid)begin
		rd_cnt <= rd_cnt + 1'b1;
	end else begin 
		rd_cnt <= rd_cnt;
	end 
end 


always @ (posedge clk )begin 
	if(rst)begin
		fast_read<=1'b0;
	end else if (read_start_flag)begin 
		fast_read<=1'b1;
	end else if(busy==1'b0)begin 
		fast_read<=1'b0;
	end else begin 
		fast_read<=fast_read;
	end 
end 	
//fast_read

always @ (posedge clk )begin 
	if(rst)begin
		rden<=1'b0;
	end else if(read_start_flag)begin 
		rden<=1'b1;
	end else if(rd_cnt>=(asmi_num+1))begin 
		rden<=1'b0;
	end else begin 
		rden<=rden;
	end 
end 
//rden

//page write ******************************************************************
always @ (posedge clk )begin 
	asmi_writestart_t <={asmi_writestart_t[0],asmi_writestart};//rise edge
	//write_start_flag  <=(asmi_writestart_t[0])&(!asmi_writestart_t[1]);//rise edge 
	data_in <= asmi_didata;
end 

always @ (posedge clk )begin 
	if(rst)begin 
		wren<=0;
	end else if((asmi_writestart_t[0])&(!asmi_writestart_t[1]))begin//rise edge 
		wren<=1'b1;
	end else if (page_write|sector_erase)begin 
		wren<=1'b0;
	end else begin 
		wren<=(wren)|((asmi_erasestart_t[0])&(!asmi_erasestart_t[1]));
	end 
end 
//wren
	
	
always @ (posedge clk )begin 
	if(wren==0)begin 
		wr_cnt <= 0;
	end else if(shift_bytes)begin
		wr_cnt <= wr_cnt + 1'b1;
	end else begin 
		wr_cnt <= wr_cnt;
	end 
end 
//wr_cnt

always @ (posedge clk )begin 
	if((wr_cnt==(asmi_num)&shift_bytes==1)| wr_cnt>(asmi_num))begin
		shift_bytes<=0;							
	end else  if(wren&asmi_divld)begin
		shift_bytes<=1;
	end else begin 
		shift_bytes<=0;
	end 
end 
//shift_bytes

always @ (posedge clk )begin 
	if(wren==0)begin
		page_write<=0;
	end else if(wr_cnt==(asmi_num+1))begin 
		page_write<=1;
	end else begin 
		page_write<=page_write;
	end 
end 
//page_write


//sector erase operation***********************************************************
always @ (posedge clk )begin 
	asmi_erasestart_t<={asmi_erasestart_t[0],asmi_erasestart};
end 

always @ (posedge clk)begin 
	sector_erase<=(asmi_erasestart_t[0])&(!asmi_erasestart_t[1]);
//	if(rst)begin 
//			sector_erase<=0;
//	end else if((asmi_erasestart_t[0])&(!asmi_erasestart_t[1]))
//			sector_erase<=1;
//	end else if (busy==0)begin 
//			sector_erase<=0;
//	end 
end 
//sector_erase


asmi_flash_ctl 	asmi_flash_ctl_inst(

		.rst_in 			( rst 				),//input 
		.clk_in 			( clk 				),//input 
		.fast_read 			( fast_read 		),//input //fast_read
		.sector_erase 		( sector_erase 		),//input 
		.page_write 		( page_write 		),//input //page_write
		.fast_read_dual 	('d0 				),//input //fast_read_dual
		.quad_fast_read 	('d0  				),//input 
		.quad_io_fast_read 	('d0     			),//input 
		.quad_page_write 	('d0 				),//input 
		.quad_enable 		('d0 				),//input 
		.rden 				( rden 				),//input 
		.wren 				( wren 				),//input 
		.shift_bytes 		( shift_bytes 		),//input 
		.datain 			(data_in  			),//input 
		.address 			( asmi_address 		),//input 
				
		.dataout 			( asmi_dataout	 	),//output
		.data_valid 		( asmi_dovalid 		),//output
		.busy 				( busy 				),//output	
		
		//SPI ports
		.miso 				( miso 				),//input
		.miso_1 			( miso_1 			),//input miso_1
		.miso_2 			( miso_2 			),//input miso_2
		.miso_3 			( miso_3 			),//input miso_3
			
		.sclk 				( sclk 				),//output
		.nss 				( nss 				),//output
		.mosi 				( mosi 				),//output
		.mosi_1 			( mosi_1 			),//output mosi_1
		.mosi_2 			( mosi_2 			),//output mosi_2
		.mosi_3 			( mosi_3 			),//output mosi_3
		.mosi_oe 			( mosi_oe 			) //output mosi_oe
	
		
);


//  efx_flash_controller_top
//     #(.ADDR_WIDTH(24), //Set the flash to support 24-bits addressing. 
//       .SCLK_DIV(3)     //MIN divider is 2. 
//       )
//     uut(
//	 // Outputs
//	 .dataout		(asmi_dataout),
//	 .data_valid	(asmi_dovalid),
//	 .busy		(busy),
//	 .sclk		(sclk),
//	 .nss		(nss),
//	 .mosi		(mosi),
//	 .mosi_oe	(mosi_oe),
//	 // Inputs
//	 .rst_in	(rst),
//	 .clk_in	(clk),
//	 .fast_read	(fast_read),
//	 .sector_erase	(sector_erase),
//	 .page_write	(page_write),
//	 .fast_read_dual(),
//	 .address	(asmi_address[24-1:0]),
//	 .rden		(rden),
//	 .wren		(wren),
//	 .shift_bytes	(shift_bytes),
//	 .datain	(data_in[7:0]),
//	 .miso		(miso),
//	 .miso_1	(miso_1));
//     
     
endmodule