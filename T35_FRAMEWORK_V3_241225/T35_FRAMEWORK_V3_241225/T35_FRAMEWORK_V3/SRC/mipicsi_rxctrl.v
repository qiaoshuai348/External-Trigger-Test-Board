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
//mipi_rx_inst1_TYPE: 37h vertical optical black line data 
//mipi_rx_inst1_TYPE: 12h embedded data
//mipi_rx_inst1_TYPE: 00h Frame start ;01h frame end ;  
//mipi_rx_inst1_TYPE: 2CH RAW12 ; 2D RAW14; 2BH RAW10  non supported RAW16
// Dependencies: 
// 
// Revision:rev2
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------



module mipicsi_rxctrl(
		
		input	wire			mipi_pclk				,//100Mhz 10ns
		input	wire			mipi_rstn				,
		
		
  // MIPI Video input
  		input 	wire [3:0] 		mipi_rx_inst1_CNT		,
  		input 	wire [63:0] 	mipi_rx_inst1_DATA		,
  		input 	wire [17:0] 	mipi_rx_inst1_ERROR		,
  		input 	wire [3:0] 		mipirxip_hsync			,//mipirxip_hsync  //mipi_rx_inst1_HSYNC
  		input 	wire [5:0] 		mipi_rx_inst1_TYPE		,//
  		input 	wire [3:0] 		mipi_rx_inst1_ULPS		,
  		input 	wire			mipi_rx_inst1_ULPS_CLK	,
  		input 	wire			mipi_rx_inst1_VALID		,
  		input 	wire [1:0] 		mipi_rx_inst1_VC		,
  		input 	wire [3:0] 		mipirxip_vsync			,//mipirxip_vsync  //mipi_rx_inst1_VSYNC
  		input	wire [7:0]		DecodeMode				,//test mode register
		input	wire [7:0]		xpatch					,//2023 08 03 
 
  // MIPI Control
  		//output  wire 			mipi_rx_inst1_CLEAR		,
  		output  reg 			mipi_rx_inst1_DPHY_RSTN=1,
  		output  wire [1:0] 		mipi_rx_inst1_LANES		,//2'B11
  		output  reg 			mipi_rx_inst1_RSTN	=1	,
  		output  wire [3:0] 		mipi_rx_inst1_VC_ENA	,
  		
  		output	reg [15:0]		DetectedXSize	=0		,
  		output	reg [15:0]		DetectedYSize	=0		,
  		output	reg	[15:0]		DetectedBw		=0		,//MB
  		output	reg [63:0]		mipi_rxdata_o	=0		,
  		output	reg 			mipi_rxvld_o	=0		,
  		output	reg				mipi_rxvsyncvld =0		,
  		output	reg 			mipi_rxhsyncvld =0		,
  		output	reg 			line_st=0				,
		output 	reg 			line_ed=0		  		,	  		  		  		
  		output	reg	            frame_st		=0		,
  		output	reg		        frame_ed  		=0		,
  		output	reg 			framed_long		=0	
  		
  		
);


assign mipi_rx_inst1_VC_ENA = 4'b1111;
assign mipi_rx_inst1_LANES = 2'b11 ;//all lanes
//assign mipi_rx_inst1_CLEAR = 1'b0;

reg [1:0]	mipi_rstn_t0 		=2'b11;
reg [13:0]  pix_cntx			=0;
reg [15:0]	pix_cnty    		=0;
reg 		hsync_t0			=0;
reg			vsync_t0			=0;


reg [26:0]	cnt_1s				=0;
reg [27:0]	DetectedBw_cnt		=0;
wire 		flag_1s				;

reg [63:0]	mipi_rxdata	=0		;
reg 		mipi_rxvld	=0		;
reg [4:0]   frame_edtn=0		;


//*********************2023 08 02 Horizontal complementer  START******************************************
reg [7:0] xpatch_r=0;
reg 	  xpatch_flag=0;
reg [7:0] xpatch_cnt=0;
reg       rowpitch_flag=0;


always @ (posedge mipi_pclk)begin
	xpatch_r<=xpatch;
end

always @ (posedge mipi_pclk)begin 
	if(line_st|frame_ed)begin
		rowpitch_flag<=0;
	end else if (line_ed)begin
		rowpitch_flag<=1;
	end else begin 
		rowpitch_flag<=rowpitch_flag;
	end 
end 
//rowpitch_flag

always @ (posedge mipi_pclk)begin
	if(mipi_rxvsyncvld==1&rowpitch_flag==1)begin
		if(xpatch_cnt<xpatch_r)begin
				xpatch_cnt<=xpatch_cnt+1;
		end else begin
				xpatch_cnt<=xpatch_cnt;
		end 
	end else begin
		xpatch_cnt<=0;
	end 
end 
//xpatch_cnt

always @ (posedge mipi_pclk)begin
	if(xpatch_cnt<xpatch_r&&mipi_rxvsyncvld==1&&rowpitch_flag==1)begin
		xpatch_flag<=1;
	end else begin
		xpatch_flag<=0;
	end 
end 
//xpatch_flag


//*********************2023 08 02 Horizontal complementer  END ******************************************


// ********************2023 07 25 Virtual Channel 1 Generate START****************************************
reg 	[0:0]		mipi_rx_inst1_VSYNC;
reg  	[0:0]		mipi_rx_inst1_HSYNC;

always @ (posedge mipi_pclk)begin
	mipi_rx_inst1_VSYNC<= | mipirxip_vsync;
	mipi_rx_inst1_HSYNC<= | mipirxip_hsync;
end 


// ********************2023 07 25 Virtual Channel 1 Generate end ****************************************


//************reset Control*****************************

always @ (posedge mipi_pclk )begin 
	mipi_rstn_t0<= {mipi_rstn_t0[0],mipi_rstn};
	mipi_rx_inst1_RSTN <= mipi_rstn_t0[1];
	mipi_rx_inst1_DPHY_RSTN<=mipi_rstn_t0[1];
end 



always @ (posedge mipi_pclk)begin 
	hsync_t0 <= mipi_rx_inst1_HSYNC[0];	
	vsync_t0 <= mipi_rx_inst1_VSYNC[0];
	
end 

always @ (posedge mipi_pclk)begin 
	line_st <= (mipi_rx_inst1_HSYNC[0]==1'b1&hsync_t0==1'b0&mipi_rxvsyncvld==1'b1)?1'b1:1'b0;
	line_ed <= (mipi_rx_inst1_HSYNC[0]==1'b0&hsync_t0==1'b1&mipi_rxvsyncvld==1'b1)?1'b1:1'b0;
	
	frame_st<= (mipi_rx_inst1_VSYNC[0]==1'b1&vsync_t0==1'b0)?1'b1:1'b0;
	frame_ed<= (mipi_rx_inst1_VSYNC[0]==1'b0&vsync_t0==1'b1)?1'b1:1'b0;
end 	
//line_st frame_st


always @ (posedge mipi_pclk)begin 
	if(mipi_rstn_t0[1]==1'b0|mipi_rx_inst1_VSYNC[0]==1'b0)begin
			mipi_rxvsyncvld <= 1'b0;
	end else if(mipi_rx_inst1_TYPE=='h2C||mipi_rx_inst1_TYPE=='h2d||mipi_rx_inst1_TYPE=='h2b||mipi_rx_inst1_TYPE=='h2a)begin 
			mipi_rxvsyncvld <= 1'b1;
	end else begin 
			mipi_rxvsyncvld <= mipi_rxvsyncvld;
	end 
end 
//mipi_rxvsyncvld

always @ (posedge mipi_pclk)begin 
	if(mipi_rstn_t0[1]==1'b0|mipi_rx_inst1_HSYNC[0]==1'b0)begin
		mipi_rxhsyncvld <= 1'b0;
	end else if(mipi_rx_inst1_HSYNC[0]==1'b1&mipi_rxvsyncvld==1'b1)begin
		mipi_rxhsyncvld <= 1'b1;
	end else begin 
		mipi_rxhsyncvld <= mipi_rxhsyncvld;
	end 
end 
//mipi_rxhsyncvld



always @ (posedge mipi_pclk)begin 
	frame_edtn <= {frame_edtn[3:0],frame_ed};
	framed_long<= |frame_edtn[4:0];
end 
//framed_long



always @ (posedge mipi_pclk )begin 
	case(mipi_rx_inst1_TYPE) 
	'h2c: begin//raw12  2C
		mipi_rxdata[15:0]  <= {mipi_rx_inst1_DATA[11:0],4'd0};
		mipi_rxdata[31:16] <= {mipi_rx_inst1_DATA[23:12],4'd0};
		mipi_rxdata[47:32] <= {mipi_rx_inst1_DATA[35:24],4'd0};
		mipi_rxdata[63:48] <= {mipi_rx_inst1_DATA[47:36],4'd0};
	end 
	'h2d: begin //raw14 2D
		mipi_rxdata[15:0]  <= {mipi_rx_inst1_DATA[13:0],2'd0};
		mipi_rxdata[31:16] <= {mipi_rx_inst1_DATA[27:14],2'd0};
		mipi_rxdata[47:32] <= {mipi_rx_inst1_DATA[41:28],2'd0};
		mipi_rxdata[63:48] <= {mipi_rx_inst1_DATA[55:42],2'd0};
	end
	'h2b: begin //RAW 10 2b
		mipi_rxdata[15:0]  <= {mipi_rx_inst1_DATA[9:0],6'd0};
		mipi_rxdata[31:16] <= {mipi_rx_inst1_DATA[19:10],6'd0};
		mipi_rxdata[47:32] <= {mipi_rx_inst1_DATA[29:20],6'd0};
		mipi_rxdata[63:48] <= {mipi_rx_inst1_DATA[39:30],6'd0};
	end 
	'h2a: begin //RAW 8 2A NOTE :need set reg03(is16bit) 1;FX3 Firmware needs to be modified£¬
		mipi_rxdata        <={
								mipi_rx_inst1_DATA[55:48], mipi_rx_inst1_DATA[63:56],
								mipi_rx_inst1_DATA[39:32], mipi_rx_inst1_DATA[47:40],
								mipi_rx_inst1_DATA[23:16], mipi_rx_inst1_DATA[31:24],
								mipi_rx_inst1_DATA[7:0]  , mipi_rx_inst1_DATA[15:8]  
							   };	//2023 11 11 ysk
	end 
	default	begin 
		mipi_rxdata		  <=0;
	end 
	endcase
	
end 	
//mipi_rxdata 

always @ (posedge mipi_pclk )begin 
	if(mipi_rxvsyncvld&mipi_rx_inst1_VALID&mipi_rxhsyncvld)begin 
		mipi_rxvld<=1'b1;
	end else if(xpatch_flag)begin
		mipi_rxvld<=1'b1;
	end else begin 
		mipi_rxvld<=0;
	end 
end 
//mipi_rxvld

always @ (posedge mipi_pclk )begin 
	if(DecodeMode[7:4]==1)begin 
		mipi_rxdata_o<=gradual_data;
		mipi_rxvld_o<=gradual_vld;
	end else begin 
		mipi_rxdata_o<={mipi_rxdata[15:0],mipi_rxdata[31:16],mipi_rxdata[47:32],mipi_rxdata[63:48]};//={mipi_rxdata[31:0],mipi_rxdata[63:32]};
		mipi_rxvld_o<=mipi_rxvld;
	end 
end 
//mipi_rxdata_o
//mipi_rxvld_o

/// *************************pix DetectedSize******************************************

always @ (posedge mipi_pclk)begin 
	if(frame_ed)begin 
		pix_cnty <= 0;
	end else if(line_st)begin 
		pix_cnty <= pix_cnty + 1'b1;
	end else begin
		pix_cnty <= pix_cnty;
	end 
end 
//pix_cnty

always @ (posedge mipi_pclk)begin 
	if(line_ed)begin 
		pix_cntx <= 0;
	end else if(mipi_rxvld)begin//(mipi_rx_inst1_VALID==1'b1&mipi_rxhsyncvld==1'b1)begin // mipi_rx_inst1_HSYNC
		pix_cntx <= pix_cntx + 1'b1;//mipi_rx_inst1_CNT
	end else begin 
		pix_cntx <= pix_cntx ;
	end 
	
end 
//pix_cntx
always @ (posedge mipi_pclk)begin 
	if(line_ed&&mipi_rx_inst1_TYPE=='h2a)begin 
		DetectedXSize <= {pix_cntx[12:0],3'b000};
	end else if(line_ed)begin 
		DetectedXSize <= {pix_cntx[13:0],2'b00};
	end else begin 
		DetectedXSize <= DetectedXSize;
	end 
end 
//DetectedXSize



always @ (posedge mipi_pclk)begin 
	if(frame_ed==1'b1)begin
		DetectedYSize <= pix_cnty;
	end else begin 
		DetectedYSize <= DetectedYSize;
	end 
end 
//DetectedYSize
/// *************************pix DetectedSize****************************************

///*************************pixTEST PIX DATA ******************************************

reg [63:0] gradual_data=0;
reg 	     gradual_vld =0;
always @ (posedge mipi_pclk)begin 
	if(line_st)begin
		//gradual_data<= {pix_cnty,pix_cnty+1'b1,pix_cnty+'d2,pix_cnty+'d3};
		gradual_data[15:0]  <=pix_cnty+2;//
		gradual_data[31:16] <=pix_cnty+1;
		gradual_data[47:32] <=pix_cnty  ;
		gradual_data[63:48] <=pix_cnty-1;		
	end else if(mipi_rxvld)begin 
		gradual_data[63:0] <=gradual_data[63:0]  +64'h0004000400040004;
	end else begin 
		gradual_data[63:0]  <= gradual_data[63:0] ;
	end 
end 	
//gradual_data
always @ (posedge mipi_pclk)begin 
	gradual_vld<=mipi_rxvld;
end 
//gradual_vld


//**************************band width test start ***********************************
always @ (posedge mipi_pclk)begin 
	if(flag_1s)begin 
			cnt_1s<= 0;
	end else begin 
			cnt_1s<= cnt_1s+1'b1;
	end 
end 
//cnt_1s

//always @ (posedge mipi_pclk)begin 
//	flag_1s <= cnt_1s[25]&cnt_1s[26];
//end 
assign flag_1s = cnt_1s[25]&cnt_1s[26];
//flag_1s

always @ (posedge mipi_pclk)begin 
	if(flag_1s)begin 
		DetectedBw_cnt <= 0;
	end else if(mipi_rxvld) begin 
		DetectedBw_cnt <= DetectedBw_cnt + 1'b1;
	end else begin 
		DetectedBw_cnt <= DetectedBw_cnt;
	end 
end 
//DetectedBw_cnt

always @ (posedge mipi_pclk)begin 
	if(flag_1s)begin 
		DetectedBw<={'d0,DetectedBw_cnt[27:17]};//MB
	end else begin 
		DetectedBw<=DetectedBw;
	end 
end 
//DetectedBw

//////////////////////////test line period cycle////////////////////////////////////////
//reg [15:0] line_cnt ;
//
//always @ (posedge mipi_pclk )begin 
//	if(line_st)begin
//		line_cnt <= 0;
//	end else if(mipi_rx_inst1_HSYNC[0])begin 
//		line_cnt <= line_cnt + 1 ;
//	end else begin 
//		line_cnt <= line_cnt;
//	end 
//end 
////line_cnt
//reg [15:0] 	frameskip_cnt = 0;
//reg  		frameskip_flag=0;
//
//always @(posedge mipi_pclk)begin 
//	if(frame_st)begin 
//		frameskip_flag<=0;
//	end else if(frame_ed)begin 
//		frameskip_flag<=1;
//	end else begin 
//		frameskip_flag<=frameskip_flag;
//	end 
//end 
////frameskijp_flag
//
//
//always @ (posedge mipi_pclk )begin
//	if (frameskip_flag)begin 
//		frameskip_cnt<=frameskip_cnt+1'b1;
//	end else begin 
//		frameskip_cnt<=0;
//	end 
//end 
////frameskip_cnt


// ********************2023 07 20 vertical effective  ob data detection START****************************************

reg vob_flag = 0;
reg [15:0] vob_cntx=0 ;
reg [15:0] vob_cnty=0 ;

always @ (posedge mipi_pclk)begin
	if(mipi_rx_inst1_TYPE=='h37)begin
		vob_flag <= 1;
	end else begin
		vob_flag<=0;
	end 
end 
//vob_flag

always @ (posedge mipi_pclk)begin
	if(mipi_rx_inst1_HSYNC[0]==0)begin
		vob_cntx<=0;
	end else if(vob_flag==1&&mipi_rx_inst1_HSYNC[0]==1&&mipi_rx_inst1_VSYNC[0]==1&&mipi_rx_inst1_VALID==1)begin
		vob_cntx<=vob_cntx+mipi_rx_inst1_CNT;
	end else begin 
		vob_cntx<=vob_cntx;
	end 
end 
//vob_cntx


always @ (posedge mipi_pclk)begin
	if(mipi_rx_inst1_VSYNC[0]==0)begin
		vob_cnty<=0;
	end else if(vob_flag==1&&mipi_rx_inst1_HSYNC[0]==1'b1&hsync_t0==1'b0&&mipi_rx_inst1_VSYNC[0]==1)begin
		vob_cnty<=vob_cnty+1;
	end else begin 
		vob_cnty<=vob_cnty;
	end 
end 
//vob_cntx


// ********************2023 07 20 vertical effective  ob data detection END ******************************

//==================================2024 09 20 YSK TEST SATER ==========================================//
`ifdef MIPIDATA_TEST
reg [31:0] FramEdDelayCnt=0;
reg [31:0] lineDelaycnt=0;

always@(posedge mipi_pclk)begin 
	if(mipi_rxvld==1)
		FramEdDelayCnt<=0;
	else  
		FramEdDelayCnt<=FramEdDelayCnt+1;
	
end 

always@(posedge mipi_pclk)begin 
	if(line_ed==1)
		lineDelaycnt<=0;
	else 
		lineDelaycnt<=lineDelaycnt+1;
end 
//lineDelaycnt
`endif 
//==================================2024 09 20 YSK TEST END ============================================//


endmodule