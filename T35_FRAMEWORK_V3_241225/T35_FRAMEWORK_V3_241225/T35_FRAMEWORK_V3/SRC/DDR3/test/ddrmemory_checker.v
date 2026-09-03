`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: Yang
// 
// Create Date: 2022/4/01 
// Design Name: TEST_TOP
// Module Name: ddrmemory_checker
// Project Name: TEST_TOP
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: 修改易灵思的demo做的读写数据测试
// Dependencies: 
// 
// Revision:rev2
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

`timescale 1ps/1ps
module ddrmemory_checker #(
         
    parameter 		AXIDATA_WIDTH 	= 127					,//e.g. 127 is [127:0]  
	parameter 		ALEN 			= 7  					,//e.g. 23 is butst Transmission 24 times
	parameter 		ASIZE 			= 4 					,//4 is 2^4 = 16bytes ,
	parameter 		START_ADDR 		= 32'h00000000			, 
	parameter 		STOP_ADDR 		= 32'h00100000			
	
)
(

	input 		 wire						axi_clk			,
	input 		 wire						rstn			,//active low  ,default0
	input 		 wire						start			,//rige edge start 
	output		 reg 						fail			,
	output		 reg 						done			,
	output		 reg	[24:0]				err_cnt			,
	
	
	input 		 wire						aready			,
	output		 wire 	[7:0] 				aid				,
	output		 reg 	[31:0] 				aaddr			,
	output		 reg 	[7:0] 				alen			,
	output		 reg 	[2:0] 				asize			,
	output		 reg 	[1:0] 				aburst			,
	output		 wire 	[1:0] 				alock			,
	output		 reg 						avalid			,
	output		 reg 						atype			,
         
	input 									wready			,		
	output		 wire	[7:0] 				wid				,
	output		 reg 	[AXIDATA_WIDTH:0] 	wdata			,
	output		 wire	[31:0] 				wstrb			,
	output		 reg 						wlast			,
	output		 reg 						wvalid			,

         		
	input 		 wire	[7:0] 				rid				,
	input 		 wire	[1:0] 				rresp			,
	input 		 wire	[AXIDATA_WIDTH:0] 	rdata			,
	input 		 wire						rlast			,
	input 		 wire						rvalid			,
	output		 reg 						rready			,
        		                    		
	input 		 wire	[7:0] 				bid				,
	input 		 wire						bvalid			,
	output		 reg 						bready			
	     		
	
	    		                        		

);



	assign aid 		= 8'h00;
	assign wstrb 	= 32'hFFFFFFFF;
	assign wid 		= 8'h00;
	assign alock 	= 2'b00;     
	
	wire	[3:0]							o_states		;
	reg 	[3:0] 							states, nstates	;
	reg 									bvalid_done		;
	reg 	[1:0] 							start_sync;
	reg 	[8:0] 							write_cnt, read_cnt;
	reg [AXIDATA_WIDTH:0] 					rdata_store		;
	reg wburst_done, rburst_done, write_done, read_done		;
 	reg 	[AXIDATA_WIDTH:0] 				obs_rdata_exp	;
  	reg 	[AXIDATA_WIDTH:0] 				obs_rdata_det	;
  	
  	///////yang add 
  	//reg   [24:0]							err_cnt		=0 ;
  	reg   [5:0]							test_times = 0 ;
  	always @(posedge axi_clk or negedge rstn) begin
  		if (!rstn) begin
  				err_cnt <= 0;
  		end else if(fail)begin 
  				err_cnt <= err_cnt + 1 ;
  		end else begin 
  				err_cnt <= err_cnt;
  		end 
  	end 
  	

  	
  	///////////////
//Main states
localparam 

	IDLE		 = 4'b0000			 , 
	WRITE_ADDR 	 = 4'b0001			 ,
	PRE_WRITE 	 = 4'b0010			 ,
	WRITE 		 = 4'b0011			 ,
	POST_WRITE 	 = 4'b0100			 ,
	READ_ADDR 	 = 4'b0101			 ,
	PRE_READ 	 = 4'b0110			 ,
	READ_COMPARE = 4'b0111			 ,
	POST_READ 	 = 4'b1000			 ,
	DONE 		 = 4'b1001			 ;
	 
wire	rstn ;

localparam   ADDR_OFFSET = ((ALEN + 1)<<ASIZE) ;//ADDR_OFFSET = (ALEN + 1)*32;       

always @(posedge axi_clk or negedge rstn) begin
	if (!rstn) begin
		start_sync <= 2'b00;
	end else begin
		start_sync[0] <= start;
		start_sync[1] <= start_sync[0];
	end
end

always @(posedge axi_clk or negedge rstn) begin
 	if (!rstn) begin
	states <= IDLE;
	end else begin
	states <= nstates;
	end
end

always @(states or start_sync[1] or write_cnt or rburst_done or write_done or read_done or bvalid_done or aready) begin
	
	case(states) 
	IDLE 	   		: if (start_sync[1]==0&start_sync[0]==1) 			nstates = WRITE_ADDR;// if (start_sync[1]==0&start_sync[0]==1) 	
	             		else						nstates = IDLE;
	             		
	WRITE_ADDR 		: if (aready)					nstates = PRE_WRITE;
		     			else						nstates = WRITE_ADDR;
		     			
	PRE_WRITE  		: 								nstates = WRITE;
	
	WRITE	   		: if (write_cnt == 9'd0)		nstates = POST_WRITE;
		     			else		 				nstates = WRITE;
		     			
	POST_WRITE 		: if (write_done & bvalid_done) nstates = READ_ADDR;
		     			else if (bvalid_done)		nstates = WRITE_ADDR;
		     			else						nstates = POST_WRITE;
		     			
	READ_ADDR  		: if (aready) 					nstates = PRE_READ;
		     			else						nstates = READ_ADDR;
		     			
	PRE_READ   		:								nstates = READ_COMPARE;
	
	READ_COMPARE  	: if (rburst_done) 				nstates = POST_READ;
						else						nstates = READ_COMPARE;
						
	POST_READ  		: if (read_done) 				nstates = DONE;
						else						nstates = READ_ADDR;
						///ysk
	DONE	   		: if(test_times[5]==1)			nstates = DONE;
						else 						nstates = IDLE;
	default											nstates = IDLE;
	
	endcase
end

always @(posedge axi_clk or negedge rstn) begin
	if (!rstn) begin
		aaddr <= START_ADDR;
		avalid <= 1'b0;
		atype <= 1'b0;
		aburst <= 2'b00;
		asize <= 3'b000;
		alen <= 8'd0;		
		wvalid <= 1'b0;
		write_cnt <= ALEN + 1;
		write_done <= 1'b0;
		wdata <= 0;
		wburst_done <= 1'b0;
		wlast <= 1'b0;
		bready <= 1'b0;
		fail <= 1'b0;
		done <= 1'b0;
		rready <= 1'b0;
		bvalid_done <=1'b0;
		obs_rdata_det <= 0;
		obs_rdata_exp <= 0;
		//ysk
		test_times <= 0 ;
		
		
	end else begin
		if (states == IDLE) begin
	                aaddr <= START_ADDR;
	                avalid <= 1'b0;
        	        atype <= 1'b0;
               	 	aburst <= 2'b00;
                	asize <= 3'b000;
                	alen <= 8'd0;               
                	wvalid <= 1'b0;
                	write_cnt <= ALEN + 1;
                	wdata <= 0;
                	wburst_done <= 1'b0;
                	wlast <= 1'b0;
                	bready <= 1'b0;
					rready <= 1'b0;
					bvalid_done <= 1'b0;
					fail <= 1'b0;
					done <= 1'b0;
		end
		if (states == WRITE_ADDR) begin
			avalid <= 1'b1;
			atype <= 1'b1;
			asize <= ASIZE;
			alen <= ALEN;
			aburst <= 2'b01;
			wvalid <= 1'b0;
			write_cnt <= ALEN + 1;
			wburst_done <= 1'b0;
			bvalid_done <= 1'b0;
			bready <= 1'b0;
			rready <= 1'b0;
			done <= 1'b0;
			fail <= 1'b0;
		end
		if (states == PRE_WRITE) begin//one clock 
			avalid <= 1'b0;
			atype <= 1'b0;
			wvalid <= 1'b1;
			wdata <= {{4{~write_cnt[7:0]}}, {4{~write_cnt[7:0]}}, {8{~write_cnt[7:0]}}};//wdata <= {aaddr, ~aaddr, {8{~write_cnt[7:0]}}, ~aaddr, aaddr, {8{write_cnt[7:0]}}};      
			bready <= 1'b1;
			write_cnt <= write_cnt - 1;
		end
		if (states == WRITE) begin
			if (wready == 1'b1) begin
                  		wdata <= {{4{~write_cnt[7:0]}}, {4{~write_cnt[7:0]}}, {8{~write_cnt[7:0]}}};//wdata <= {aaddr, ~aaddr, {8{~write_cnt[7:0]}}, ~aaddr, aaddr, {8{write_cnt[7:0]}}};
				if (write_cnt == 9'd0) begin
						wburst_done <= 1'b1;
						wlast <= 1'b0;
						wvalid <= 1'b0;
				if (aaddr >= STOP_ADDR) begin
						write_done <= 1'b1;
				end else begin
						write_done <= 1'b0;
				end
				end if (write_cnt == 9'd1) begin
						wlast <= 1'b1;
						write_cnt <= write_cnt - 1;
				end else begin
						write_cnt <= write_cnt - 1;
				end
			end
		end
		if (states == POST_WRITE) begin
			if (write_done) begin
				aaddr <= START_ADDR;
			end else begin
				if (bvalid) begin// bvalid wlast_t0
				aaddr <= aaddr + ADDR_OFFSET;
				end
			end
			if (wready == 1'b1) begin
				wlast <= 1'b0;	
				wvalid <= 1'b0;	
			end
			if (bvalid) begin//bvalid wlast_t0
				bvalid_done <= 1'b1;
				bready <= 1'b0;
			end
			end
		if (states == READ_ADDR) begin
			avalid <= 1'b1;
			read_cnt <= ALEN + 1;
				
		end
		if (states == PRE_READ) begin
			avalid <= 1'b0;
			rburst_done <= 1'b0;
            rdata_store <= {{4{~read_cnt[7:0]}}, {4{~read_cnt[7:0]}}, {8{~read_cnt[7:0]}}};//rdata_store <= {aaddr, ~aaddr, {8{~read_cnt[7:0]}},~aaddr,aaddr,{8{read_cnt[7:0]}}};
		    read_cnt <= read_cnt - 1'b1;
		end
		if (states == READ_COMPARE) begin
			rready <= 1'b1;
			if (read_cnt != 9'd0) begin
			if (rvalid == 1'b1) begin
                        rdata_store <= {{4{~read_cnt[7:0]}}, {4{~read_cnt[7:0]}}, {8{~read_cnt[7:0]}}};//rdata_store <= {aaddr, ~aaddr, {8{~read_cnt[7:0]}},~aaddr,aaddr,{8{read_cnt[7:0]}}};
			read_cnt <= read_cnt - 1'b1;
				if (rdata != rdata_store) begin
					fail <= 1'b1;
					obs_rdata_exp <= rdata_store;
					obs_rdata_det <= rdata;
					//`ifdef EFX_SIM
					//$display("ERROR!! Read mismatch : read = 0x%x, expected = 0x%x",rdata,rdata_store);
					//`endif 
				//end else begin
					//`ifdef EFX_SIM
					//$display("Read match: read = 0x%x, expected = 0x%x",rdata,rdata_store);
					//`endif
				end
	
			end
			end
			if (read_cnt == 9'd0) begin
	                        if (rvalid == 1'b1) begin
                                       if (rdata != rdata_store) begin
                                                fail <= 1'b1;
												obs_rdata_exp <= rdata_store;
												obs_rdata_det <= rdata;
                                                //`ifdef EFX_SIM
                                               // $display("ERROR!! Read mismatch : read = 0x%x, expected = 0x%x",rdata,rdata_store);
                                                //`endif
                                        //end else begin
                                                //`ifdef EFX_SIM
                                                //$display("Read match: read = 0x%x, expected = 0x%x",rdata,rdata_store);
                                               // `endif
                                        end


					if (aaddr >= STOP_ADDR) begin
						read_done <= 1'b1;
					end else begin
						read_done <= 1'b0;
					end
					rburst_done <= 1'b1;
				end
			end	
		end
		if (states == POST_READ) begin
			aaddr <= aaddr + ADDR_OFFSET;
			rready <= 1'b1;
		end
		if (states == DONE) begin
			//ysk
			if(test_times[5]==1)begin
					done <= 1'b1;
			end else begin
				test_times<=test_times + 1;
			end 
		end
	end

end


assign o_states = states;
reg 	wlast_t0;
always @ (posedge axi_clk)begin 
	wlast_t0<=wlast;
end 

//// test 

reg [23:0] cnt100ms =0;
reg [31:0] data_num	=0;
reg 	   test_done=0;
	

always @ (posedge axi_clk )begin
	if(start==1&&test_done==0)begin 
	    //if (cnt100ms[23]==1&&cnt100ms[20]==1&&cnt100ms[19]==1)begin 
		//	cnt100ms<=0;
		//end else if begin 
			cnt100ms<=cnt100ms+1;
		//end 
	end else begin 
		cnt100ms<=0;
	end 
end 
//cnt100ms


always @ (posedge axi_clk )begin
	if(start==0)begin 
		test_done<=0;
	end else if(cnt100ms[23]==1&&cnt100ms[20]==1&&cnt100ms[19]==1)begin 
		test_done<=1;
	end else begin 
		test_done<= test_done;
	end 
end 
//test_done

always @ (posedge axi_clk )begin 
	if(start==0)begin 
		data_num<=0;
	end else if(test_done)begin 
		data_num<=data_num;
	end else  if(wvalid)begin
		data_num<=data_num+1;
	end else begin 
		data_num<=data_num;
	end 
end 
//data_num

	


endmodule
