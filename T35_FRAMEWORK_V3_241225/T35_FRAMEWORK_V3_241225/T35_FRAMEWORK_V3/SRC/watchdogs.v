////////////////////////////////////////////////////////////////////////////////
// Company: QHYCCD
// Engineer: YangSK
//
// Create Date: 2022 04 11   /220530 Transplant to here    // 220519 updata               
// Design Name: watch dogs ,for fx3 reset    
// Module Name: 
// Target Device: T35 
// Tool versions: 
// Description:                              
// <Description here>
// Dependencies:                             
// <Dependencies here>
// Revision: V1.1                                  
// <Code_revision_information>
// Additional Comments:
// <Additional comments>
////////////////////////////////////////////////////////////////////////////////

module watchdogs(

		input	wire	  clk					,//25M 40ns
		input	wire	  watchdogenable		,//connect to reg89,reg89 initial value is 1
		input	wire	  driver_feed_dogs		,//connect to reg1x03 ,reg1x03 initial value is 0	
			
		output	reg 	  FX3_RST_N	=  1		,
		output  reg [7:0] rstn_num	=0			 //connect to myreg 210;
		
);


reg 				dri_feed_dogs_t0=	0;
reg 				dri_feed_dogs_t1=	0;
reg 				dri_feed_dogs_t2=	0;
reg 				driver_rst_n	=	1;
reg					FX3_RST_N_t		=	1;

reg 	[31:0]		driver_rstn_cnt	=	0;//feed dogs times cnt  ;about 32s  [28]==1 [29]==1  ,if fx3  NOT feed dog, after,fx3 will reset 
reg		[17:0]		rstn_cnt		=	0;//fx3 reset times cnt  ;about 5ms  [14]==1 [15]==1 ,reset will continue about 2ms 

initial begin 
	dri_feed_dogs_t0<=0;
	dri_feed_dogs_t1<=0;
	dri_feed_dogs_t2<=0;
	FX3_RST_N <= 1;
	FX3_RST_N_t<=1;
	driver_rst_n<=1;
	rstn_num<=0;
	rstn_cnt<=0;
	driver_rstn_cnt<=0;
	
end 
always @ (posedge clk)begin 
	if(rstn_num[7]==1'b1)begin 
		rstn_num<=rstn_num;
	end else if(FX3_RST_N_t==1&&FX3_RST_N==0)begin 
		rstn_num<=rstn_num+1'b1;
	end else begin 
		rstn_num<=rstn_num;
	end 
end 
//rstn_num
	

always @ (posedge clk )begin 
	
	dri_feed_dogs_t0<=driver_feed_dogs;
	dri_feed_dogs_t1<=dri_feed_dogs_t0;
		
	FX3_RST_N_t <= FX3_RST_N;
end 


`ifdef CloseWatchDog
 always @ (posedge clk )begin  
 	FX3_RST_N <= 1; 
end 
`else 
	
always @ (posedge clk )begin 
	if(watchdogenable)begin
		FX3_RST_N <= driver_rst_n;
	end else begin 
		FX3_RST_N <= 1;
	end 
end 
//FX3_RST_N
`endif


always @ (posedge clk )begin 
	if(FX3_RST_N==0)begin 
		rstn_cnt <= rstn_cnt + 1 ;
	end else begin 
		rstn_cnt <= 0;
	end 
end 
//rstn_cnt


always @ (posedge clk )begin 
	if(watchdogenable==0)begin
		dri_feed_dogs_t2<=1;
	end else if(dri_feed_dogs_t0==1&dri_feed_dogs_t1==0)begin //rise edge 驱动喂狗动作
		dri_feed_dogs_t2<=1;
	end else begin 
		dri_feed_dogs_t2<=0;
	end 
end 
//dri_feed_dogs_t2

always @ (posedge clk )begin 
	if(dri_feed_dogs_t2)begin //driver_feed_dogs
		driver_rstn_cnt <= 0;
	end else if (FX3_RST_N==0)begin 
		driver_rstn_cnt <= 0;
	end else begin 
		driver_rstn_cnt <= driver_rstn_cnt + 1 ;
	end 
end 
//driver_rstn_cnt

always @ (posedge clk )begin 
	if (dri_feed_dogs_t2)begin//driver_feed_dogs
		driver_rst_n <= 1;
	end else if (rstn_cnt[17]==1)begin//rstn_cnt[14]==1&&
		driver_rst_n <= 1;
	end else if (driver_rstn_cnt[29]==1&&driver_rstn_cnt[28]==1)begin //32s
		driver_rst_n <= 0;
	end else begin 
		driver_rst_n <= driver_rst_n ;
	end 
end 
//driver_rst_n


endmodule 