

module testddr_data(

input	wire 		clk			,
input	wire		rst			,
input	wire		en			,
input	wire  [63:0]rd_ddrdata	,
input	wire 		rd_ddrvld 	,


output	reg [63:0] data	=0  	,
output	reg 	   vld	=0		,
output	reg 	   verify_err=0

);

reg 	 verify_st	=0;
//reg   	 vsync	=0;
//reg 	 hsync  =0;
//
//reg [15:0] h_cnt =0 ;
//
//reg [15:0] v_cnt =0 ;
//
//always @ (posedge clk )begin 
//	if(h_cnt[13]==1&h_cnt[12]==1&
//	end else if(hsync==1)begin
//		 h_cnt<=h_cnt+ 1;
//	end else begin 
//		h_cnt <=h_cnt;
//	end 
//end 
////h_cnt
//
//always @ (posedge clk )begin
//	if(h_cnt[13]==1)begin
//		hsync<=0;
//	end else if(h_cnt[1]==1)begin
//		hsync<=1;
//	end else begin 
//		hsync <= hsync;
//	end 
//end 
////hsync

wire LowSpeed_75  ;
reg LowSpeed_50p =0 ;
reg LowSpeed_50n=0 ;

always@ (posedge clk )begin 
	LowSpeed_50p <=~LowSpeed_50p;
end 

always @ (negedge clk )begin
	LowSpeed_50n <= ~LowSpeed_50n;
end 

assign LowSpeed_75 = LowSpeed_50n|LowSpeed_50p;


always @ (posedge clk )begin 
	if(rst==1|en==0)begin 
		vld<=0;
	end else begin
		vld<=~vld;
	end 
end 
//vld

always @ (posedge clk )begin 
	if(rst==1|en==0)begin 
		data<=0;
	end else if(vld)begin
		data<=data+1;
	end 
end 
//data 

always @ (posedge clk )begin
	if(rst)begin
		verify_st <= 1'b0;
	end else if(rd_ddrvld)begin
		verify_st <=1'b1;
	end else begin 
		verify_st<=verify_st;
	end 
end 
//verify_st
//reg 	   verify_err=0;
reg [63:0]  rd_ddrdata_t0=0;
reg 	 	verify_st	=0;

always @ (posedge clk)begin
	if(rd_ddrvld)begin
		rd_ddrdata_t0<=rd_ddrdata;
	end else begin
		rd_ddrdata_t0<=rd_ddrdata_t0;
	end 
end 
//rd_ddrdata_t0

always @(posedge clk )begin
	if(verify_st==1&&rd_ddrvld==1&&rd_ddrdata[15:0]<65533&&en==1)begin
			if(rd_ddrdata[15:0]-rd_ddrdata_t0[15:0]==1'b1)begin
				verify_err<=1'b0;
			end else begin 
				verify_err<=1'b1;
			end 
	end else begin 
		verify_err <= 1'b0;
	end 
end 
//verify_err

endmodule 