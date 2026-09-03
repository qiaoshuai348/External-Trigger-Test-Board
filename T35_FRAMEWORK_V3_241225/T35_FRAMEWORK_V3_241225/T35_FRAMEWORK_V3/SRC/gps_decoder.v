module gps_decoder (

	input	wire		gps_clk		,
	input	wire		pix_clk		,
	input	wire		gps_rst		,//active high 
	input	wire		gps_in		,
	
	
	output	reg  [31:0] gps1		,
	output	reg  [31:0] gps2		,
	output	reg  [31:0] gps3		,
	output	reg  [31:0] gps4		,
	output	reg  [31:0] gps5		,
	output	reg  [31:0] gps6		,
	output	reg  [31:0] gps7		,
	output	reg  [31:0] gps8		,
	output	reg  [31:0] gps9		

);

	//reg 	[7:0]   test_data	=0	;
    reg 	[31:0] 	gps_para		;
	reg 			gps_valid		;
	reg 	[4:0] 	cnt				;
	reg 	[3:0] 	counter			;
	reg				read_enable	=0	;
	
	wire 	   		gps_fifo_out	;
	wire		    gps_fifo_empty  ;

//gps_clk

//	always @ (posedge gps_clk )begin 
//		test_data <= test_data + 1;
//	end 
//test_data



///////////////////////////////////////////////////////////////////////////////////////////
	always @(posedge pix_clk)
	begin
	   if(gps_fifo_empty==0)   read_enable<=1;
		else                   read_enable<=0;
	end 
//	read_enable	
	
	always @(posedge pix_clk)
	begin
	   if(read_enable==1) gps_para[31:0] <= {gps_para[30:0], gps_fifo_out};
	   else gps_para[31:0] <= gps_para[31:0] ;
	end
//	gps_para
	
	always @(posedge pix_clk)
	begin
		if      (gps_para[31:0] == 32'hEE11DD22 && read_enable==1)   gps_valid <= 1'd1;
		else if (gps_para[31:0] == 32'h22DD11EE && read_enable==1)   gps_valid <= 1'd0;
		else                                                         gps_valid <= gps_valid;
	end	
//generate gps_valid signal

	
	always @(posedge pix_clk)
	begin
		if(gps_valid)
        begin
	       if(read_enable==1) 	  cnt[4:0] <= cnt[4:0] + 1'd1;	
		    else                  cnt[4:0] <= cnt[4:0];
		  end
		else      	              cnt[4:0] <= 5'd0;
	end
	
	
	
	always @(posedge pix_clk)
	begin
		if(gps_valid && read_enable==1 && cnt[4:0] == 5'd31)
		begin
			counter[3:0] <= counter[3:0] + 1'd1;
			if(counter[3:0] == 4'd10) counter[3:0] <= 4'd0;
		end
		else if(!gps_valid) counter[3:0] <= 4'd0;
	end
	
	
	
	always @(posedge pix_clk)
	begin
		if(gps_valid && read_enable==1 && cnt[4:0] == 5'd31)
		begin
			case(counter)
				4'd0  :  gps1[31:0] <= gps_para[31:0];
				4'd1  :  gps2[31:0] <= gps_para[31:0];
				4'd2  :  gps3[31:0] <= gps_para[31:0];
				4'd3  :  gps4[31:0] <= gps_para[31:0];
				4'd4  :  gps5[31:0] <= gps_para[31:0];
				4'd5  :  gps6[31:0] <= gps_para[31:0];
				4'd6  :  gps7[31:0] <= gps_para[31:0];
				4'd7  :  gps8[31:0] <= gps_para[31:0];
				4'd8  :  gps9[31:0] <= gps_para[31:0];
				//4'd9  :  gps10[31:0] <= gps_para[31:0];
			endcase
		end
	end
	 
	
	//generate the read_valid signal. This is based on the experiement on waveform. fifo is set to latch=2 and normal mode(not show a head)
	
		
//	GPS_FIFO GPS_FIFO_INST (
//		.data    (gps_in				),   //   input,  width = 9,  fifo_input.datain
//		.wrreq   (1'b1					),   //   input,  width = 1,            .wrreq
//		.rdreq   (1'b1					),   //   input,  width = 1,            .rdreq
//		.wrclk   (gps_clk				),   //   input,  width = 1,            .wrclk
//		.rdclk   (pix_clk				),   //   input,  width = 1,  //always read. use empty to determind if valid. 
//		.q       (gps_fifo_out			),   //  output,  width = 9, fifo_output.dataout
//		.rdempty (gps_fifo_empty		),   //  output,  width = 1,            .rdempty
//		.wrfull  (						)    //  output,  width = 1,            .wrfull
//	);
	
	
   GPS_FIFO u_GPS_FIFO(
   
		.full_o 		(  		 		),
		.empty_o 		( gps_fifo_empty),
		.rdata 			( gps_fifo_out 	),
		.wr_clk_i 		( gps_clk 		),
		.rd_clk_i 		( pix_clk 		),
		.wr_en_i 		( 1'b1 		    ),
		.rd_en_i 		( 1'b1 		    ),
		.a_rst_i 		( 1'b0 		    ),
		.wdata 			( gps_in 		),
		.wr_datacount_o (  				),
		.rd_datacount_o (  				),
		.rst_busy 		(  				)
		
);
		
	//-----------------------------------------------------------------------------
	
	
	
	
	
	//---------------------GPS RAW Data Receive Code------------------------------
	
	/*
	

	
	reg [31:0] GPS_RAW_ShiftRegister;
	reg gps_raw_valid;
	reg [4:0] cnt_raw;
	reg [7:0] counter_raw;   //256*32BIT  =1kbyte
	
	//shift register move in
	always @(posedge cmos_inck)
	begin
		GPS_RAW_ShiftRegister[31:0] <= {gps_in,GPS_RAW_ShiftRegister[31:1]};   //shift register for GPS RAW serial data
	end
	
	
	//detect signal head and end for gps raw data
	always @(posedge cmos_inck)
	begin
		if     (GPS_RAW_ShiftRegister == 32'hEE33CC44)   gps_raw_valid <= 1'd1;          //GPS serial data head detected
		else if(GPS_RAW_ShiftRegister == 32'h44CC33EE)   gps_raw_valid <= 1'd0;          //GPS serial data end detected
		else                                             gps_raw_valid <= gps_raw_valid;
	end	  
	
   //raw clock (bit) counter (0-31)
	always @(posedge cmos_inck)
	begin
		if(gps_raw_valid)  cnt_raw<= cnt_raw + 1;
		else               cnt_raw<= 0;
	end
	
	//raw word counter  (32bit)
	always @(posedge cmos_inck)
	begin
		if(gps_raw_valid && cnt_raw[4:0] == 5'd31)
		begin
			counter_raw <= counter_raw + 1;
			//if(counter_raw == 255)   counter_raw <= 0;
		end
		else if(!gps_raw_valid) counter_raw <= 0;
	end
	
	
	always @(posedge cmos_inck)
	begin
		if(gps_raw_valid && cnt_raw[4:0] == 5'd31)
		begin
			ram_address_wr <= counter_raw;
			ram_dataIn     <= GPS_RAW_ShiftRegister;
		end
	end
	
	
	//from the signalTapeII we see there is a unstable value at 31 , so we use a stable range to make the wren
	//generate the wren signal during cnt is in the range of 20-25
	always @(posedge cmos_inck)
	begin
		if(gps_raw_valid==1) 
		  begin
		  if (cnt_raw < 20 || cnt_raw > 25)         ram_wren<=0;
	     else                                      ram_wren<=1;
		end	
	end
	



	
   reg [31:0] ram_dataIn;
	wire [31:0] ram_dataOut;
	reg ram_wren;
	reg ram_rden;
	reg [7:0] ram_address_wr;
	reg [7:0] ram_address_rd;
	
	

	
	RAM_bb u_ram(
	
	.data(ram_dataIn),
	.rdaddress(ram_address_rd),
	.rdclock(cmos_inck),
	.rden(ram_rden),
	
	.wraddress(ram_address_wr),
	.wrclock(cmos_inck),
	.wren(ram_wren),
	.q(ram_dataOut)
   );
	
	*/


endmodule