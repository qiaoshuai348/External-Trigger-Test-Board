`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/5/25
// Design Name: T35_TOP
// Module Name: tb_ddr3_state
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description:   ddr3 :MT41K256M16TW-107:P£»  1.07ns @ CL = 13 (DDR3-1866) -107  ; row 15bit ;  colum 10bit  bank 3bit ,512Mbyte,addre is 29bit
// Dependencies:  DDR3 ADDRESS[27:0] =2^28*16/1024/1024/1024=4Gbit; AXI ADDRESS is [28:0];every address is 8bit data; 0-1fff_ffff
// 
// Revision:rev1 
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------
    
 `define      RDFIFI_MAX 		     512                  
 `define      DDR3ADDRE_MAXBIT     	 28                   
 `define      ALEN_0		     	 64                
 `define      STOP_ADDR_0	     	 16384  //test is 2048 ,normal is  32'h1FFFFFFF,check done address    Reducing this parameter speeds up ddr initialization   
 `define      START_ADDR_0           32'H0	              
 `define 	  DDR3ADDRE_MAX			 16384  //test is 2048 ,normal is  32'h1FFFFFFF,ddr3 max address ,E.P.: AXI ADDRESS is [28:0] =   32'h1FFFFFFF 
 `define 	  COUNT_ENDBIT 			 8     // TEST is8 ,normal is 12
   
    
module tb_ddr3_state();


reg	clk = 0 ; 
reg ddr_reset_done = 0 ;
reg DDR_CLG_R = 1 ; 

reg [63:0] 	wrddr3_data =0;
reg 		wrddr3_en	=0;

reg   			DDR_CTRL_AREADY_0		=1;					
reg   			DDR_CTRL_BVALID_0	    =1;
reg   [127:0]	DDR_CTRL_RDATA_0		=0;			
reg   			DDR_CTRL_RLAST_0		=0;			
reg   			DDR_CTRL_RVALID_0	    =0;
reg   			DDR_CTRL_WREADY_0	    =1;   
reg  			vld_flag	=0;
reg [7:0]		vld_cnt		=0;
    

wire    [5:0]			ddr_code				;
wire 	[31:0] 	    	DDR_CTRL_AADDR_0		;
wire 	[1:0] 	    	DDR_CTRL_ABURST_0	    ;
wire 	[7:0] 	    	DDR_CTRL_AID_0		    ;
wire 	[7:0] 	    	DDR_CTRL_ALEN_0	        ;
wire 	[1:0] 	    	DDR_CTRL_ALOCK_0	    ;
wire 	[2:0] 	    	DDR_CTRL_ASIZE_0	    ;
wire 					DDR_CTRL_ATYPE_0	    ;
wire 					DDR_CTRL_AVALID_0	    ;
wire 					DDR_CTRL_BREADY_0	    ;
wire 					DDR_CTRL_RREADY_0	    ;
wire 	[127:0] 		DDR_CTRL_WDATA_0	    ;
wire 	[7:0] 	    	DDR_CTRL_WID_0		    ;
wire 					DDR_CTRL_WLAST_0	    ;
wire 	[15:0] 	    	DDR_CTRL_WSTRB_0	    ;
wire 					DDR_CTRL_WVALID_0       ;


wire [63:0]	rddr3_data	;
wire		rddr3_vld	;


reg [127:0] test_data=0;
reg 		test_vld =0;
reg 		test_rst =1;

//
wire		rdfifo_full1;
wire		rdfifo_empty1;
wire [63:0]	rddr3_data1	;
wire		rst_busy;

always #10 clk = ~clk ;


initial begin
	#100;
	ddr_reset_done<=1;
	DDR_CLG_R <=0;
	test_rst<=0;
	
end 


always @ (posedge clk )begin 
//	if(wrddr3_data[7:0]==8)begin
//		wrddr3_data <=wrddr3_data;
//		wrddr3_en  <=0;
//	end else
	 if(ddr_reset_done==1&ddr_code[0]==0)begin //ddr_code[5]==0&&
		wrddr3_data <= wrddr3_data + 64'h0101_0101_0101_0101;
		wrddr3_en	<= 1'b1;
	end else begin 
		wrddr3_data <= wrddr3_data ;
		wrddr3_en	<= 1'b0;
	end 
end 
//wrddr3_data wrddr3_en



always @ (posedge clk )begin 
	if(vld_cnt==DDR_CTRL_ALEN_0)begin
		vld_flag <= 0;
		DDR_CTRL_RLAST_0<=1;
	end else if(DDR_CTRL_ATYPE_0==0&&DDR_CTRL_AVALID_0==1)begin
		vld_flag <= 1;
		DDR_CTRL_RLAST_0<=0;
	end else begin 
		vld_flag <= vld_flag;
		DDR_CTRL_RLAST_0<=0;
	end 
end 

//
always @ (posedge clk )begin 
	if(vld_flag)begin
		vld_cnt <= vld_cnt + 1 ;
	end else begin
		vld_cnt <= 0;
	end 
end 

always @ (posedge clk )begin
	if(vld_flag)begin
		DDR_CTRL_RDATA_0 <= {~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,
						     ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt,  ~vld_cnt };//DDR_CTRL_RDATA_0+64'h0101_0101_0101_0101;
		DDR_CTRL_RVALID_0<=1;
	end else begin 
		DDR_CTRL_RDATA_0 <= DDR_CTRL_RDATA_0;
		DDR_CTRL_RVALID_0<=0;
	end 
end 
//


always @ (posedge clk)begin
	if(rst_busy==0)begin
		test_vld<=1;
	end else begin 
		test_vld<=0;
	end 
end 

rdddr3_fifo 	tbtest_rdddr3_fifo(
	
	.wr_clk_i		( clk 				),//
	.rd_clk_i 		( clk 				),//
	.wr_en_i 		( test_vld	 		),//
	.rd_en_i 		( 0 				),//
	.a_rst_i 		( test_rst			),//reset the FIFO if check_done ==0;
	.wdata 			( test_data 		),//
	
	.full_o 		( rdfifo_full1 		),//
	.empty_o		( rdfifo_empty1 	),
	.rdata 			( rddr3_data1 		),//
	.wr_datacount_o ( 					),//
	.rd_datacount_o ( 					),
	.rst_busy 		( rst_busy		 	)

);

 ddr3_state  ddr3_state_inst(

		.clk					(clk				),
		.reset_done				(ddr_reset_done		),
		.DDR_CLG_R				(DDR_CLG_R			),
		.ddrpll_locked			(1'b1				),
	                  
		.wr_clk					(clk			    ),
		.wrddr3_data			(wrddr3_data		),
		.wrddr3_en				(wrddr3_en		    ),
		.rd_clk					(clk			    ),
		.rddr3_en				(0				    ),//
		.fx3_full				(0				    ),	
		.threshold_num			(2048			    ),//unit: byte			
		.rddr3_data				(rddr3_data		    ),
		.rddr3_vld				(rddr3_vld		    ),
		                                                        
		//ddr3 state signals		                            
		.rema_num				(					),//unit:byte £¬set it to 256bit
		.ddr_code 				(ddr_code			),//{ddr_pfull,ddr_alempty,ddr_alfull,addr_err,check_fail,check_done};                                                               
		
		//ddr axi signals				                            
		.DDR_CTRL_AREADY_0		(DDR_CTRL_AREADY_0	),  //**Address ready.
  		.DDR_CTRL_BID_0			(0					), 
  		.DDR_CTRL_BVALID_0		(DDR_CTRL_BVALID_0	),  //Write response valid. This signal indicates that the channel is signaling a valid write response.                  
  		.DDR_CTRL_RDATA_0		(DDR_CTRL_RDATA_0	),  //**Read data.              
  		.DDR_CTRL_RID_0			(0					),
  		.DDR_CTRL_RLAST_0		(DDR_CTRL_RLAST_0	),  //**Read last. This signal indicates the last transfer in a read burst.                      
  		.DDR_CTRL_RRESP_0		(0					),  //Read response. This signal indicates the status of the read transfer
  		.DDR_CTRL_RVALID_0		(DDR_CTRL_RVALID_0	),  //**Read valid
  		.DDR_CTRL_WREADY_0		(DDR_CTRL_WREADY_0	),  //**Write ready. This signal indicates that the slave can accept the write data.
  		                		                                                                                         
  		.DDR_CTRL_AADDR_0		(DDR_CTRL_AADDR_0	 ),  //** Address. ATYPE defines whether it is a read or write address. It gives the address of the first transfer in a burst transaction. 
  		.DDR_CTRL_ABURST_0		(DDR_CTRL_ABURST_0	 ),  // Burst type. The burst type and the size determine how the address  for each transfer within the burst is calculated.               
  		.DDR_CTRL_AID_0			(DDR_CTRL_AID_0		 ),  // Address ID. This signal identifies the group of address signals. Depends on ATYPE, the ID can be for a read or write address group             
  		.DDR_CTRL_ALEN_0		(DDR_CTRL_ALEN_0	 ),  // Burst length. This signal indicates the number of transfers in a burst
  		.DDR_CTRL_ALOCK_0		(DDR_CTRL_ALOCK_0	 ),  // Lock type. This signal provides additional information about the  atomic characteristics of the transfer  
  		.DDR_CTRL_ASIZE_0		(DDR_CTRL_ASIZE_0	 ),  // Burst size. This signal indicates the size of each transfer in the burst.                       
  		.DDR_CTRL_ATYPE_0		(DDR_CTRL_ATYPE_0	 ),  //** This signal distinguishes whether is it is a read or write operation. 0= read and 1 = write.
  		.DDR_CTRL_AVALID_0		(DDR_CTRL_AVALID_0	 ),  //** Address valid. This signal indicates that the channel is signaling valid address and control information.                                                
  		.DDR_CTRL_BREADY_0		(DDR_CTRL_BREADY_0	 ),  // Response ready. This signal indicates that the master can accept a write response                            
  		.DDR_CTRL_RREADY_0		(DDR_CTRL_RREADY_0	 ),  //** Read ready. This signal indicates that the master can accept the read data and response information.                                              
  		.DDR_CTRL_WDATA_0		(DDR_CTRL_WDATA_0	 ),  //** Write data.
  		.DDR_CTRL_WID_0			(DDR_CTRL_WID_0		 ),  // Write ID tag. This signal is the ID tag of the write data transfer
  		.DDR_CTRL_WLAST_0		(DDR_CTRL_WLAST_0	 ),  //** Write last. This signal indicates the last transfer in a write burst
  		.DDR_CTRL_WSTRB_0		(DDR_CTRL_WSTRB_0	 ),  // Write strobes. This signal indicates which byte lanes hold valid data. There is one write strobe bit for each eight bits of the write data bus  
  		.DDR_CTRL_WVALID_0   	(DDR_CTRL_WVALID_0   )   //** Writ

);


endmodule 