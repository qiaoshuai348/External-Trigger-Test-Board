`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/4/6
// Design Name: T35_TOP
// Module Name: data_process
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


module data_process(

	input	wire			clk				,
	input	wire			rst				,//bypass Dgain_process,active high 
	input	wire			is16bit			,
	input	wire	[63:0]	mipi_rxdata		,
	input	wire			mipi_rxvld		,
	input	wire			mipi_line_ed	,
	input	wire			frame_st		,
	input	wire			frame_ed		,
	input   wire  	[7:0]   gain_mode 		,
	input	wire	[7:0]	gain_18			,//
	input	wire	[7:0]	gain_19			,//
	input	wire	[7:0]	gain_20			,//
	input 	wire    [7:0]	gain_21			,//
	input	wire    [5:0]	mipi_rx_inst1_TYPE,
	input   wire 	[1:0] 	mipi_rx_inst1_VC,
	
	
	input	wire			ResetFrameCount ,
	input	wire			FrameNumEn		,///
	
	input	wire			EnableBurstMode ,///
	input	wire	[31:0]	PatchVNumber	,///
	input	wire	[7 :0]  BurstStart		,///
	input	wire	[15:0]  BurstEnd  		,///
	
	input	wire			ddr_pfull		,///
	input	wire			isddr			,
	
	input	wire			gps_enable		,
	input	wire	[15:0]  DetectedYSize	,
	input	wire	[15:0]  DetectedXSize	,
	input	wire  	[31:0]  gps1			,
	input	wire  	[31:0]  gps2			,
	input	wire  	[31:0]  gps3			,
	input	wire  	[31:0]  gps4			,
	input	wire  	[31:0]  gps5			,
	input	wire  	[31:0]  gps6			,
	input	wire  	[31:0]  gps7			,
	input	wire  	[31:0]  gps8			,
	input	wire  	[31:0]  gps9			,
	
	
	output	reg [63:0]		Data64_after	=0		,
	output	reg 			Data64Wr_after=0	
	
);

//not use 
reg [47:0] timer_stamp  = 0 ;
wire[15:0] hsize_3;
assign hsize_3 = {DetectedXSize[12:0],3'd0};//pix per line
//

reg 		frame_drop=0;
reg [4:0]   gpsadd_cnt=0;
reg 		speed_cut_flag	=0;
reg 		single_patch_flag =0;
reg 		isFrameEndPatch =0;   
reg [31:0] 	counterEnd =0;
reg 		frame_edt0	;
reg 		frame_edt1  ;

reg [31:0]  framecount=0;
reg   		lineflag=0;
reg [7:0]	gainA 	=8;
reg [7:0]	gainB 	=8;
reg [63:0]	data_raw16=0;
reg [63:0]	data_raw8=0;
reg 		data_raw8_flag=0;
reg 		data_raw8_wr=0;

reg [63:0]  Data64=0;
reg 		Data64Wr=0;

reg [63:0]  Data64_f=0;
reg 		Data64Wr_f=0;
 
//reg [63:0] Data64_t	   ;
//reg 	   Data64Wr_t=0;
		

wire [23:0] tempDataA,tempDataB,tempDataC,tempDataD;

reg 		mipi_rxvld_t0=0;
reg 		mipi_rxvld_t1=0;
reg 		mipi_rxvld_t2=0;
reg 		mipi_rxvld_t3=0;

reg 		frame_stt0	;
reg 		frame_stt1	;
	

always @ (posedge clk)begin 
	mipi_rxvld_t0<=mipi_rxvld;
	mipi_rxvld_t1<=mipi_rxvld_t0;
	mipi_rxvld_t2<=mipi_rxvld_t1;
	mipi_rxvld_t3<=mipi_rxvld_t2;
	
	frame_edt0 <= frame_ed;
	frame_edt1 <= frame_edt0;
	
	frame_stt0 <= frame_st;
	frame_stt1 <= frame_stt0;
end 
//mipi_rxvld_t0




//=========================================2024 12 25 start==========================================//


always @ (posedge clk)begin 
	if(frame_st)begin 
		lineflag<=0;
	end else if (mipi_line_ed==1&&mipi_rx_inst1_VC[0]==1'b1&&gain_mode[1]==1'b1)begin
		lineflag<=~lineflag;
	end else if (mipi_line_ed==1&&mipi_rx_inst1_VC[0]==1'b0&&gain_mode[1]==1'b0)begin 
		lineflag<=~lineflag;
	end else begin 
		lineflag<=lineflag;
	end 
end //lineflag

//mipi_rx_inst1_VC
/*
always @ (posedge clk)begin 
	if(frame_st)begin 
		lineflag<=0;
	end else if (mipi_line_ed==1)begin 
		lineflag<=~lineflag;
	end else begin 
		lineflag<=lineflag;
	end 
end 
//linecnt
*/

//=========================================2024 12 25 end==========================================//

//****************************************old code start ***********************************//
/*
always @(posedge clk) begin
  if(lineflag ==0)begin  
  	gainA<=gain_g;gainB<=gain_r;  
  end else begin  
  	gainA<=gain_b;gainB<=gain_g2;  //gain_b
  end
end 
//gainA gainB

always @(posedge clk) begin
  //if(lineflag ==0)begin  
  if(frame_stt1 ==1)begin  //YSK 2023 07 19 ,GAIN Takes effect at NEST frame
  	gainA<=gain_18;gainB<=gain_19;  
  end else begin  
  	gainA<=gain_20;gainB<=gain_21;  
  end
end 
//gainA gainB
*/
//****************************************old code end ***********************************//


reg [7:0] gain_18_t0=8;
reg [7:0] gain_19_t0=8;
reg	[7:0] gain_20_t0=8;
reg [7:0] gain_21_t0=8;


always @(posedge clk) begin
	 if(frame_stt1 ==1)begin 
		gain_18_t0<= gain_18;
		gain_19_t0<= gain_19;
		gain_20_t0<= gain_20;
		gain_21_t0<= gain_21;
	 end else begin
		gain_18_t0<=gain_18_t0;
		gain_19_t0<=gain_19_t0;
		gain_20_t0<=gain_20_t0;
		gain_21_t0<=gain_21_t0;
	 end 
end 

always @(posedge clk) begin
  if(lineflag ==0)begin  
  	gainA<=gain_18_t0;gainB<=gain_19_t0;  
  end else begin  
  	gainA<=gain_20_t0;gainB<=gain_21_t0;  
  end
end 
//gainA gainB YSK 2023 10 31 


 unsigned_reg_mult
#(
	 .WIDTHA		(16					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_instA
(
   .clk				(clk				),
   .a				(mipi_rxdata[15:0]	),
   .b				(gainA				),
   .o               (tempDataA			)
);
//tempDataA



 unsigned_reg_mult
#(
	 .WIDTHA		(16					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_instB
(
   .clk				(clk				),
   .a				(mipi_rxdata[31:16]	),
   .b				(gainB				),
   .o               (tempDataB			)
);
//tempDataB


 unsigned_reg_mult
#(
	 .WIDTHA		(16					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_instC
(
   .clk				(clk				),
   .a				(mipi_rxdata[47:32]	),
   .b				(gainA				),
   .o               (tempDataC			)
);
//tempDataC


 unsigned_reg_mult
#(
	 .WIDTHA		(16					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_instD
(
   .clk				(clk				),
   .a				(mipi_rxdata[63:48]	),
   .b				(gainB				),
   .o               (tempDataD			)//vaild is mipi_rxvld_t1
);
//tempDataD


always @(posedge clk)
begin

if(gain_mode[0]==1)begin//FPGA GAIN==64 is pix gain 1.0
		
	 		if (tempDataA>24'h3FFFC0)  	  data_raw16[15:0]<=16'hffff;
				else                      data_raw16[15:0]<=tempDataA[21:6];
	        
  			 if (tempDataB>24'h3FFFC0)     data_raw16[31:16]<=16'hffff;
				else                      data_raw16[31:16]<=tempDataB[21:6];
				
	         if (tempDataC>24'h3FFFC0)     data_raw16[47:32]<=16'hffff;	
				else                      data_raw16[47:32]<=tempDataC[21:6];
				
	         if (tempDataD>24'h3FFFC0)     data_raw16[63:48]<=16'hffff;
                else                      data_raw16[63:48]<=tempDataD[21:6];	
end	else begin//FPGA GAIN==8 is pix gain 1.0
		
	         if (tempDataA>24'h7fff8)  	  data_raw16[15:0]<=16'hffff;
				else                      data_raw16[15:0]<=tempDataA[18:3];
	        
  			 if (tempDataB>24'h7fff8)     data_raw16[31:16]<=16'hffff;
				else                      data_raw16[31:16]<=tempDataB[18:3];
				
	         if (tempDataC>24'h7fff8)     data_raw16[47:32]<=16'hffff;	
				else                      data_raw16[47:32]<=tempDataC[18:3];
				
	         if (tempDataD>24'h7fff8)     data_raw16[63:48]<=16'hffff;
                else                      data_raw16[63:48]<=tempDataD[18:3];	
	

end
	 	
end
//data_raw16

//*****************************2023 11 11 add MIPI raw8 data gain start **********************************//

wire  [15:0] tempData0,tempData1,tempData2,tempData3,tempData4,tempData5,tempData6,tempData7 ;
reg [63:0] raw16_mipi8=0;

 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst0
(
   .clk				(clk				),
   .a				(mipi_rxdata[7:0]	),
   .b				(gainA				),
   .o               (tempData0			)
);
//tempData0



 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst1
(
   .clk				(clk				),
   .a				(mipi_rxdata[15:8]	),
   .b				(gainB				),
   .o               (tempData1			)
);
//tempData1


 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst2
(
   .clk				(clk				),
   .a				(mipi_rxdata[23:16]	),
   .b				(gainA				),
   .o               (tempData2			)
);
//tempData2


 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst3
(
   .clk				(clk				),
   .a				(mipi_rxdata[31:24]	),
   .b				(gainB				),
   .o               (tempData3			)//vaild is mipi_rxvld_t1
);
//tempData3

 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst4
(
   .clk				(clk				),
   .a				(mipi_rxdata[39:32]	),
   .b				(gainA				),
   .o               (tempData4			)
);
//tempData4



 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst5
(
   .clk				(clk				),
   .a				(mipi_rxdata[47:40]	),
   .b				(gainB				),
   .o               (tempData5			)
);
//tempData5


 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst6
(
   .clk				(clk				),
   .a				(mipi_rxdata[55:48]	),
   .b				(gainA				),
   .o               (tempData6			)
);
//tempData6


 unsigned_reg_mult
#(
	 .WIDTHA		(8					),
	 .WIDTHB		(8					)
)unsigned_reg_mult_inst7
(
   .clk				(clk				),
   .a				(mipi_rxdata[63:56]	),
   .b				(gainB				),
   .o               (tempData7			)//vaild is mipi_rxvld_t1
);
//tempData7

always @(posedge clk)
begin

if(gain_mode[0]==1)begin//FPGA GAIN==64 is pix gain 1.0

	 		 if (tempData0>16'h3FC0)  	  raw16_mipi8[7:0]<=8'hff;
				else                      raw16_mipi8[7:0]<=tempData0[13:6];
	        
  			 if (tempData1>16'h3FC0)      raw16_mipi8[15:8]<=8'hff;
				else                      raw16_mipi8[15:8]<=tempData1[13:6];
				
	         if (tempData2>16'h3FC0)      raw16_mipi8[23:16]<=8'hff;	
				else                      raw16_mipi8[23:16]<=tempData2[13:6];
				
	         if (tempData3>16'h3FC0)      raw16_mipi8[31:24]<=8'hff;
                else                      raw16_mipi8[31:24]<=tempData3[13:6];

				////
			 if (tempData4>16'h3FC0)  	  raw16_mipi8[39:32]<=8'hff;
				else                      raw16_mipi8[39:32]<=tempData4[13:6];
	        
  			 if (tempData5>16'h3FC0)      raw16_mipi8[47:40]<=8'hff;
				else                      raw16_mipi8[47:40]<=tempData5[13:6];
				
	         if (tempData6>16'h3FC0)      raw16_mipi8[55:48]<=8'hff;	
				else                      raw16_mipi8[55:48]<=tempData6[13:6];
				
	         if (tempData7>16'h3FC0)      raw16_mipi8[63:56]<=8'hff;
                else                      raw16_mipi8[63:56]<=tempData7[13:6];

end	else begin//FPGA GAIN==8 is pix gain 1.0
				
	 		 if (tempData0>16'h7f8)  	  raw16_mipi8[7:0]<=8'hff;
				else                      raw16_mipi8[7:0]<=tempData0[10:3];
	        
  			 if (tempData1>16'h7f8)       raw16_mipi8[15:8]<=8'hff;
				else                      raw16_mipi8[15:8]<=tempData1[10:3];
				
	         if (tempData2>16'h7f8)       raw16_mipi8[23:16]<=8'hff;	
				else                      raw16_mipi8[23:16]<=tempData2[10:3];
				
	         if (tempData3>16'h7f8)       raw16_mipi8[31:24]<=8'hff;
                else                      raw16_mipi8[31:24]<=tempData3[10:3];

				////
			 if (tempData4>16'h7f8)  	  raw16_mipi8[39:32]<=8'hff;
				else                      raw16_mipi8[39:32]<=tempData4[10:3];
	        
  			 if (tempData5>16'h7f8)       raw16_mipi8[47:40]<=8'hff;
				else                      raw16_mipi8[47:40]<=tempData5[10:3];
				
	         if (tempData6>16'h7f8)       raw16_mipi8[55:48]<=8'hff;	
				else                      raw16_mipi8[55:48]<=tempData6[10:3];
				
	         if (tempData7>16'h7f8)       raw16_mipi8[63:56]<=8'hff;
                else                      raw16_mipi8[63:56]<=tempData7[10:3];

	      

end
	 	
end
//raw16_mipi8

//*****************************************end************************************************************//


always @ (posedge clk)begin 
	if(mipi_rxvld_t2)begin 
		data_raw8<={data_raw8[31:0],data_raw16[63:56],data_raw16[47:40],data_raw16[31:24],data_raw16[15:8]};
	end else begin 
		data_raw8<=data_raw8;
	end 
end 
//data_raw8

always @ (posedge clk)begin 
	if(rst|frame_st)begin 
		data_raw8_flag<=0;
	end else if(mipi_rxvld_t2)begin 
		data_raw8_flag<= ~data_raw8_flag;
	end else begin 
		data_raw8_flag<=data_raw8_flag;
	end 
end 
//data_raw8_flag

always @ (posedge clk)begin 
	if(rst|frame_st)begin 
		data_raw8_wr<=0;
	end else if(mipi_rxvld_t2&data_raw8_flag)begin 
		data_raw8_wr<=1;
	end else begin 
		data_raw8_wr<=0;
	end 
end 
//data_raw8_wr

always@(posedge clk)begin 
	if(mipi_rx_inst1_TYPE==6'h2A)begin
		Data64<=raw16_mipi8;
		Data64Wr<=mipi_rxvld_t2;
	end else if(is16bit)begin
		Data64<=data_raw16;
		Data64Wr<=mipi_rxvld_t2;
	end else begin 
		Data64<=data_raw8;
		Data64Wr<=data_raw8_wr;
	end 
end 
//Data64 Data64Wr


always@(posedge clk)begin
	if(frame_stt1)begin 
		Data64_f<=64'h55554444EE11DD22;
	end else if(frame_ed)begin 
		Data64_f<=64'hEE11DD2266667777;
	end else begin 
		Data64_f<= Data64;
	end 
end 

//always@(posedge clk)begin
//	if(framecount[15:0]<=BurstStart && EnableBurstMode==1&&framecount[15:0]>=BurstEnd) begin     
//		Data64Wr_f <= 0;
//	end else if(frame_st|frame_ed|isFrameEndPatch)begin 
//		Data64Wr_f <= 1;
//    end else begin
//		Data64Wr_f <=Data64Wr;//&(~frame_drop);//Data64Wr&(~frame_drop)
//	end 
//end 
//Data64_t  Data64Wr_t

always@(posedge clk)begin
	if( EnableBurstMode==1&&framecount<=BurstStart)begin
		Data64Wr_f <= 0;
	end else if(EnableBurstMode==1&&framecount>=BurstEnd&&single_patch_flag==0)begin
		Data64Wr_f <= 0;
	end else if(frame_stt1|frame_ed)begin 
		Data64Wr_f <= 1;
	end else if(single_patch_flag)begin
		Data64Wr_f <=isFrameEndPatch;
    end else begin
		Data64Wr_f <=Data64Wr;//&(~frame_drop);//Data64Wr&(~frame_drop)
	end 
end 
//Data64Wr_f


always @(posedge clk)begin
	if(ResetFrameCount==0)begin
		framecount <=0;
	end else if(frame_st)begin
		framecount <= framecount+1'b1;
	end else begin
		framecount <= framecount;
	end 
end 
//framecount

//*****************82022 05 31 Add gps data ***************************************************8


always @ (posedge clk )begin 
	if (rst|frame_st)begin 
		gpsadd_cnt <= 0;
	end else if (Data64Wr_f==1&gpsadd_cnt[4]==0)begin 
		gpsadd_cnt <= gpsadd_cnt + 1 ;
	end else begin 
		gpsadd_cnt <= gpsadd_cnt ;
	end 
end 
//usb_cnt 

always @ (posedge clk )begin 
	// if(FrameNumEn==1&&gpsadd_cnt==1)begin
	// 	Data64_after <= {framecount,framecount};
	// end else 
	if (gps_enable&&Data64Wr_f )begin	
			case(gpsadd_cnt)
					'd1   : Data64_after<={32'h01234567,framecount[31:0]};
					'd2   : Data64_after<={5'd0,framecount[2:0],hsize_3,DetectedYSize[15:0],gps1[31:8]};//
					'd3   : Data64_after<={gps1[7:0],gps2[31:8],gps2[7:0],gps3[31:8]};
					'd4   : Data64_after<={gps3[7:0],gps4[31:8],gps4[7:0],gps5[31:8]};
					'd5   : Data64_after<={gps5[7:0],gps6[31:8],gps6[7:0],gps7[31:8]};
					'd6   : Data64_after<={gps7[7:0],gps8[31:8],gps8[7:0],gps9[31:8]};
					'd7   : Data64_after<={gps9[7:0],timer_stamp[47:24],timer_stamp[23:0],8'h0a};
					'd8   : Data64_after<={32'hf40bf40b,32'hf30cf30c};
			   default    : Data64_after<=Data64_f;
			endcase
	end 
   else begin 
   	Data64_after <= Data64_f ;
   end 
end 
//Data64_after


always @ (posedge clk )begin 
	Data64Wr_after <= Data64Wr_f&(~frame_drop);
end 
//Data64Wr_after



//***************************Single Mode or Burst Mode  data complement*****************************************8

always@(posedge clk)begin 
if (counterEnd>=PatchVNumber||frame_stt0==1)begin 
		single_patch_flag <=0;
//end else if (EnableBurstMode==1&&frame_edt1==1&&framecount>BurstStart)begin //framecount==(BurstEnd-1)&&
end else if (EnableBurstMode==1&&frame_edt1==1&&framecount<=(BurstEnd+1))begin //
		single_patch_flag <=1; 
end else begin 
	   single_patch_flag <= single_patch_flag ;
end 
end 
//single_patch_flag 2021 02 01 

always@(posedge clk)
begin
  if(single_patch_flag==0) 			 speed_cut_flag<=0;
  else  
    begin
	  if(counterEnd<PatchVNumber)       speed_cut_flag<= ~speed_cut_flag;
     else                      			speed_cut_flag<=  speed_cut_flag;
    end
end
//speed_cut_flag



always@(posedge clk)
begin
  if(single_patch_flag==0) counterEnd<=0;//single_patch_flag==0||isFrameHead==1
  else  
    begin
	  if(counterEnd<PatchVNumber&&speed_cut_flag==1)     counterEnd<=counterEnd+1;
     else                      						     counterEnd<=counterEnd;
    end
end
//counterEnd


always@(posedge clk)
begin
  if(counterEnd<PatchVNumber&&speed_cut_flag==1&&single_patch_flag==1) isFrameEndPatch<=1;
  else                                  						 isFrameEndPatch<=0;
end
//isFrameEndPatch

//A flag (speed_cut_flag) has been added to slow down the rate of data added at the end of the frame in single-frame mode


//************************If DDr is full, frames are lost*********************************************************

always @(posedge clk )begin 
	if(isddr==0)begin
		frame_drop <= 0;
	end else if (frame_st&&ddr_pfull)begin
		frame_drop <= 1 ;//USB normal mode frame_drop<= 1;in USB debug mode ,always frame_drop =0;   
	end else if (frame_ed==1&&ddr_pfull== 0)begin 
		frame_drop <= 0;
	end else begin 
		frame_drop <= frame_drop;
	end 
end 
//frame_drop


endmodule 