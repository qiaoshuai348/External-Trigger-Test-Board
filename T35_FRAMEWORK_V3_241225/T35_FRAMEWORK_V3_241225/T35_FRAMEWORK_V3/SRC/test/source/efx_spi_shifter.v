/////////////////////////////////////////////////////////////////////////////
//           _____       
//          / _______    Copyright (C) 2013-2020 Efinix Inc. All rights reserved.
//         / /       \   
//        / /  ..    /   efx_spi_shifter.v
//       / / .'     /    
//    __/ /.'      /     Description:
//   __   \       /      flash controller FSM to shift the serial data in and out to SPI interface
//  /_/ /\ \_____/ /     
// ____/  \_______/      
//
// *******************************
// Revisions:
// 1.0 Initial rev
//
// *******************************

`resetall
`timescale 1 ps/1 ps
`include "dbg_defines.v"

  module efx_spi_shifter
    #(
      parameter SCLK_FREQ  = 6'd5,
      parameter ADDR_WIDTH = 24
      )
   (
    input 		   nrst,
    input 		   clkin,
    
    // to PLL
    input 		   locked,
    output 		   pll_rst,
    
    // command interface - spi_cmd
    //1 soft_reset,         // soft reset. 
    //2 read,               // read 
    //3 read_device_id,     // read device id
    //4 read_unique_id,     // read unique id
    //5 read_reg,	    // read status register 1/2/3; data_in as command
    //6 read_jedec_id,      // read jedec id.
    //8 write_enable,	    // write enable (0x06)
    //9 write_reg1,	    // write status register 1
    //10 write_reg2,	    // write status register 2
    //11 write_reg3,	    // write status register 3 
    //12 write,             // Page Program 
    //13 erase,             // Ease
    //14 read_reg1          // read status register 1
    //15 read_reg2          // read status register 2
    //16 read_reg3          // read status register 3
    //17 read_manufacturing_id // read manufacturing id.
    //18 write_enable_for_volatile,	    // write volatile status (0x50)
    //19 write_disable                      // write disable (0x04)
    //20 fast_read           //Fast Read 
    input 		   spi_cmd_en, //To enable the spi_cmd opcode command. 
    input [4:0] 	   spi_cmd,
    input [7:0] 	   spi_cmd_instr, 
    input [ADDR_WIDTH-1:0] address,
    input [7:0] 	   data_in, // command or data to input
    input [7:0] 	   burst_data_wr,
    input [8:0] 	   byte_cnt,
    input [7:0] 	   w_burst_size, //SPI Burst Write Size. 1 - 256, 2 - 512
    input [7:0] 	   r_burst_size, //SPI Burst Read Size.  1 - 256, 2 - 512
    input 		   rden,
    output [7:0] 	   data_out,
    output 		   busy,
    output 		   data_valid,

    //Output
    output reg [23:0] 	   jedec_id_reg,
    output reg [7:0] 	   manufacturing_id_reg,
    output reg [63:0] 	   unique_id_reg,
    output reg [7:0] 	   device_id_reg,
    output reg [7:0] 	   spi_flash_rd_status_reg0 ,
    output reg [7:0] 	   spi_flash_rd_status_reg1 ,
    output reg [7:0] 	   spi_flash_rd_status_reg2 ,

    output reg 		   erase_en, //This bit indicate erase command is enabled. When user issue write enable command, this bit set to high.
                              //When user isse write disable command, this bit set to low.
    output [4:0] 	   fsm_status,
    output wire 	   wfifo_rd,
    output reg 		   page_program_done,
    // SPI interface
    input 		   miso, // SPI MISO
    input 		   miso_1, //SPI MISO
    output 		   sclk, // SPI SCLK
    output 		   nss, // SPI SS
    output 		   mosi,    // SPI MOSI
    output                 mosi_oe  //active high output enable
    );
   

///////////////////////////////////////
//FSM Definition
///////////////////////////////////////
reg [4:0] state;

//State Machine  
localparam IDLE             = 5'd0, 
  READ_CMD                  = 5'd1, 
  READ_DEVICE_ID_CMD        = 5'd2, 
  READ_JEDEC_ID_CMD         = 5'd4, 
  READ_UNIQUE_ID_CMD        = 5'd5, 
  RESET_CMD                 = 5'd6, 
  WRITE_CMD                 = 5'd7, 
  ERASE_CMD                 = 5'd8, 
  WRITE_DATA                = 5'd9, 
  READ_DATA                 = 5'd10, 
  WRITE_ENABLE_CMD          = 5'd11, 
  WRITE_REG_CMD             = 5'd12, 
  READ_REG_CMD              = 5'd13,
  READ_MANUFACTURING_ID_CMD = 5'd14,
  WRITE_ENABLE_VOL_CMD      = 5'd15,
  WRITE_DISABLE_CMD         = 5'd16,
  WBURST_CHK_STATUS         = 5'd17,
  WBURST_WEL_CLR            = 5'd18,
  WBURST_WE_INSTR           = 5'd19,
  WBURST_PAGE               = 5'd20,
  FAST_READ_CMD             = 5'd21,
  READ_DUMMY_DATA           = 5'd22;
 
   
   
   

//Command Setting    
localparam RESETDATA1 = 8'h66, //Enable Reset
  RESETDATA2          = 8'h99, //Reset Device 
  UNIQUEID_DATA       = 8'h4B, //Read Unique ID
  DEVICEID_DATA       = 8'hAB, //Release Power Down / Device ID. 
  WRITEDATA           = 8'h02, //Page Program 
  WRREG1_DATA         = 8'h01, //Write Status Register-1
  WRREG2_DATA         = 8'h31, //Write Status Register-2
  WRREG3_DATA         = 8'h11, //Write Status Register-3
  RDREG1_DATA         = 8'h05, //Read Status Register-1
  RDREG2_DATA         = 8'h35, //Read Status Register-2
  RDREG3_DATA         = 8'h15, //Read Status Register-3
  JEDECID_DATA        = 8'h9F, //Read JEDEC ID
  MANUFACTURING_ID_DATA = 8'h90, // Read manufacturing id. 
  WRITE_ENABLE_DATA     = 8'h06,              //Write Enable Instruction 
  WRITE_ENABLE_VOLATILE_STAT_DATA = 8'h50,    //Write Enable for Volatile Status Register 
  WRITE_DISABLE_DATA = 8'h04,                 //Write Disable.  
  BREAD_DATA = 8'h03,
  FAST_BREAD_DATA = 8'h0B,
  FAST_BREAD_DATA_DUAL = 8'h3B,
  CHIP_ERASE_0 = 8'hc7, //CHIP ERASE Command type 0
  CHIP_ERASE_1 = 8'h60, //Chip Erase Command type 1
  SECTOR_ERASE = 8'h20,       //
  BLOCK_32KB_ERASE = 8'h52,   //
  BLOCK_64KB_ERASE = 8'hD8;   //
   
   function integer depth2width;
      input [31:0] 	  depth;
      begin
	 if (depth > 1) begin
	    depth = depth - 1;
	    for (depth2width=0; depth>0; depth2width = depth2width + 1)
	      depth = depth>>1;
	 end
	 else
	   depth2width = 0;
      end
   endfunction

   localparam RBIT_CNT_SIZE = depth2width(`RFIFO_DEPTH_256*256*8);
   localparam WBIT_CNT_SIZE = depth2width(`WFIFO_DEPTH_256*256*8);
   
//Internal Signals    
   reg [7:0] cmd_in;
   reg [5:0] cnt,cnt_0;
   reg [5:0] sclkcnt;
   reg [RBIT_CNT_SIZE-1:0] readcnt;
   reg [WBIT_CNT_SIZE-1:0] writecnt;
   wire [RBIT_CNT_SIZE-1:0] r_burst_cnt_max;
   wire [WBIT_CNT_SIZE-1:0] w_burst_cnt_max;
   reg 	      sclk_reg, sclk_reg_d;
   reg 	      read_done;
   reg 	      mosi_reg, nss_reg, busy_reg, data_valid_reg;
   reg [7:0]  data_in_reg, data_out_reg, data_out_reg_d;
   reg [ADDR_WIDTH-1:0] address_reg;
   reg [ADDR_WIDTH-1:0] flash_address_reg;
   
   reg 	      spi_cmd_en0,spi_cmd_en1;
   wire       spi_cmd_en_pos;
   reg 	      burst_wr_flag;
   reg 	      burst_rd_flag;
   
   reg 	      wfifo_rd0,wfifo_rd1;
   reg 	      data_valid_reg0;
   reg 	      data_valid_reg1;
   reg [5:0]  cnt_max;
   reg [7:0]  spi_cmd_instr_reg;
   wire       sclk_reg_neg;
   wire       sclk_reg_pos;
   
   assign fsm_status =  state;

   //19 Feb 2020
   //CL
   assign mosi = mosi_reg;
   assign mosi_oe = (state!=READ_DATA && state!=IDLE);
   //assign mosi = (state==READ_DATA) ? 1'bz : mosi_reg;
   assign nss = nss_reg;
   assign busy = busy_reg;
   assign data_valid = (burst_rd_flag) ? data_valid_reg0 & ~data_valid_reg1 : 1'b0;
   assign data_out = data_out_reg_d;
   assign pll_rst = nrst;

//////////////////////////////
// SCLK Clock Generation
//////////////////////////////
  
   always @(posedge clkin)
     begin
	if (!nrst) begin
	   sclkcnt <= 6'd0;
	   sclk_reg <= 1'b0;
	   sclk_reg_d <= 1'b0;
	end
	else begin
	   if (sclkcnt == SCLK_FREQ) begin
	      sclk_reg <= ~sclk_reg;
	      sclkcnt <= 6'd0;
	   end
	   else begin
	      sclkcnt <= sclkcnt + 1'b1;
	   end
	   sclk_reg_d <= sclk_reg;
	end
     end // always @ (posedge clkin)
   
   assign sclk = sclk_reg;   
   assign sclk_reg_neg = (sclk_reg == 1'b0 && sclk_reg_d == 1'b1);
   assign sclk_reg_pos = (sclk_reg == 1'b1 && sclk_reg_d == 1'b0);
   
     
   //To decode the command instruction counter.
   always @(*)
     begin
	case (spi_cmd_instr[7:0])
	  CHIP_ERASE_0 : cnt_max[5:0] = 6'd8;
	  CHIP_ERASE_1 : cnt_max[5:0] = 6'd8;
	  default      : cnt_max[5:0] = (6'd8 + ADDR_WIDTH); //Sector/Block Erase.
	endcase // case (spi_cmd_instr[7:0])
     end
   
   
   always @(posedge clkin)
     if (~nrst)
       begin
	  spi_cmd_en0 <= 1'b0;
	  spi_cmd_en1 <= 1'b0;
       end
   
     else 
       begin
	  spi_cmd_en0 <= spi_cmd_en;
	  spi_cmd_en1 <= spi_cmd_en0;
       end // else: !if(~nrst)

   assign spi_cmd_en_pos = spi_cmd_en0 & ~spi_cmd_en1;
   
  
   
   ////////////////////////////
   //Burst Data Size
   ////////////////////////////

   //For write burst, it's always multiple of 256 bytes. 
   //assign w_burst_cnt_max = 'd256;
   assign w_burst_cnt_max = byte_cnt;
   
   //r_burst_size is no. of words multiple of 256
   //Translate to bits r_burst_size*256*8
 // always @(*) begin 
 //    case (r_burst_size)
 //	8'h01  : r_burst_cnt_max = 'd2048 - 1;
 //	8'h02  : r_burst_cnt_max = 'd4096 - 1;
 //	8'h0A  : r_burst_cnt_max = 'd20480 - 1;
 //	8'd20  : r_burst_cnt_max = 'd40960 - 1;
 //	8'd60 : r_burst_cnt_max = 'd122880 - 1;
 //	8'd100 : r_burst_cnt_max = 'd204800 - 1;
 //	default : r_burst_cnt_max = 'd2048 - 1;
 //    endcase // case (r_burst_size)   
 //  end // always @ (*)
   
   assign r_burst_cnt_max = {r_burst_size,11'b0} -1;
   
  //w_burst_size
  //>1 - the state machine will change the burst loop state.
   reg state_loop_flag;
   reg [7:0] spi_wr_burst_size_cnt;
   reg [3:0] instr_wait_cnt;
   
   // SPI state machine
   always @(posedge clkin)
     begin
	if (!nrst) begin
	   nss_reg <= 1'b1;
	   mosi_reg <= 1'b1;
	   busy_reg <= 1'b1;
	   data_valid_reg <= 1'b0;
	   data_out_reg <= 8'd0;
	   data_out_reg_d <= 8'd0;
	   address_reg <= 'h0;
	   flash_address_reg <= 'h0;
	   
	   data_in_reg <= 8'd0;
	   state <= IDLE;
	   erase_en <= 1'b0;
	   wfifo_rd0 <= 1'b0;
	   burst_wr_flag <= 1'b0;
	   burst_rd_flag <= 1'b0;
	   page_program_done <= 1'b0;
	   spi_cmd_instr_reg <= 8'h0;
	   writecnt <= {{WBIT_CNT_SIZE}*{1'b0}};
	   readcnt <= {{RBIT_CNT_SIZE}*{1'b0}};
	   cnt <= 6'd0;
	   read_done <= 1'b0;
	   state_loop_flag <= 1'b0;
	   spi_wr_burst_size_cnt <= 8'h1;
	   instr_wait_cnt <= 4'h0;
	   
	end
	else begin
	   case (state)
	     IDLE: begin
		// SPI signals
		nss_reg <= 1'b1;
		mosi_reg <= 1'b1;
		instr_wait_cnt <= 4'h0;
		busy_reg <= 1'b0;
		readcnt <= 'b0;
		read_done <= 1'b0;
		cnt <= 6'd0;
		
		data_in_reg <= data_in;
		address_reg <= address;
		flash_address_reg <= address;
		data_valid_reg <= 1'b0;
		
		// state change
		if (spi_cmd_en_pos)
		  begin 
		     case (spi_cmd)
		       5'd1: begin	// soft reset 0x1
			  busy_reg <= 1'b1;
			  cmd_in <= RESETDATA1;
			  state <= RESET_CMD;
		       end
		       5'd2: begin	// read data 0x2
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  state <= READ_CMD;
			  cmd_in <= BREAD_DATA; //Burst Read Data
			  burst_rd_flag <= 1'b1;
		       end
		       5'd3: begin	// read device id 0x3
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= DEVICEID_DATA;
			  state <= READ_DEVICE_ID_CMD;
		       end
		       5'd4: begin	// read unique id 0x4
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= UNIQUEID_DATA;
			  state <= READ_UNIQUE_ID_CMD;
		       end
		       5'd5: begin	// read status register 0x5 - data_in as command
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= data_in[7:0];
			  state <= READ_REG_CMD;
		       end
		       5'd6: begin	// read JEDEC id 0x6
			  busy_reg <= 1'b1;
			  cmd_in <= JEDECID_DATA;
			  state <= READ_JEDEC_ID_CMD;
		       end
		       5'd8: begin	// write enable 0x8
			  busy_reg <= 1'b1;
			  cmd_in <= WRITE_ENABLE_DATA;
			  state <= WRITE_ENABLE_CMD;
			  erase_en <= 1'b1;
		       end
		       5'd9: begin	// write status reg 1
			  busy_reg <= 1'b1;
			  cmd_in <= WRREG1_DATA;
			  state <= WRITE_REG_CMD;
		       end
		       5'd10: begin	// write status reg 2
			  busy_reg <= 1'b1;
			  cmd_in <= WRREG2_DATA;
			  state <= WRITE_REG_CMD;
		       end
		       5'd11: begin	// write status reg 3
			  busy_reg <= 1'b1;
			  cmd_in <= WRREG3_DATA;
			  state <= WRITE_REG_CMD;
		       end
		       5'd12: begin	// page program 0xC
			  busy_reg <= 1'b1;
			  cmd_in <= WRITEDATA;
			  state <= WRITE_CMD;
			  wfifo_rd0 <= 1'b1;
			  burst_wr_flag <= 1'b1;
			  spi_wr_burst_size_cnt <= w_burst_size;
		       end
		       5'd13: begin	// erase page 0xD
			  busy_reg <= 1'b1;
			  state <= ERASE_CMD;
			  spi_cmd_instr_reg <= spi_cmd_instr;
			  
		       end
		       5'd14: begin	// read status register 0x05 - data_in as command
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= RDREG1_DATA;
			  state <= READ_REG_CMD;
		       end
		       5'd15: begin	// read status register 0x35 - data_in as command
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= RDREG2_DATA;
			  state <= READ_REG_CMD;
		       end
		       5'd16: begin	// read status register 0x15 - data_in as command
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= RDREG3_DATA;
			  state <= READ_REG_CMD;
		       end

		       5'd17: begin	// read manufacturing id
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  cmd_in <= MANUFACTURING_ID_DATA;
			  state <= READ_MANUFACTURING_ID_CMD;
		       end

		       5'd18: begin	// write volatile status enable 0x18
			  busy_reg <= 1'b1;
			  cmd_in <= WRITE_ENABLE_VOLATILE_STAT_DATA;
			  state <= WRITE_ENABLE_VOL_CMD;
		       end
		       
		       5'd19: begin	// write disable 0x19
			  busy_reg <= 1'b1;
			  cmd_in <= WRITE_DISABLE_DATA;
			  state <= WRITE_DISABLE_CMD;
			  erase_en <= 1'b0;
		       end

		       5'd20: begin // fast read
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  state <= FAST_READ_CMD;
			  cmd_in <= FAST_BREAD_DATA; //Burst Read Data
			  burst_rd_flag <= 1'b0;
		       end

		       5'd21: begin // fast read
			  data_out_reg_d <= 8'd0;
			  busy_reg <= 1'b1;
			  state <= FAST_READ_CMD;
			  cmd_in <= FAST_BREAD_DATA_DUAL; //Burst Read Data
			  burst_rd_flag <= 1'b0;
		       end
		       
		     endcase // case (spi_cmd)

		     page_program_done <= 1'b0;
		  end // if (spi_cmd_en)
		
		else begin
		   busy_reg <= 1'b0;
		   cmd_in <= 8'h00;
		   wfifo_rd0 <= 1'b0;
		   state <= IDLE;		   
		end // else: !if(spi_cmd_en)
	       
	     end // case: IDLE

	     

	     //Burst Read Data with 1 page (256 bytes). 
	     READ_CMD: begin			// data_in with read buffer command 0x03
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];	
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < (6'd8 + ADDR_WIDTH) ) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= address_reg[ADDR_WIDTH-1];
		      address_reg <= {address_reg[ADDR_WIDTH-2:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == (6'd8 + ADDR_WIDTH)) begin
		      readcnt <= r_burst_cnt_max;	// read 256 bytes (2048 bits) data
		      cnt <= 6'd0;
		      state <= READ_DATA;
		   end
		end
	     end // case: READ_CMD


	     //Burst Read Data with 1 page (256 bytes). 
	     FAST_READ_CMD: begin			// data_in with read buffer command 0x03
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];	
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < (6'd8 + ADDR_WIDTH)) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= address_reg[ADDR_WIDTH-1];
		      address_reg <= {address_reg[ADDR_WIDTH-2:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == (6'd8 + ADDR_WIDTH)) begin
		      readcnt <= 'd7;	// read 256 bytes (2048 bits) data
		      cnt <= 6'd0;
		      state <= READ_DUMMY_DATA;
		   end
		end
	     end
	     
	     READ_DEVICE_ID_CMD: begin	// send command 0xAB
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   nss_reg <= 1'b0;
		   if (cnt < 6'd8) begin
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < 6'd32) begin
		      mosi_reg <= 1'b0;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd32) begin
		      cnt <= 6'd0;
		      readcnt <= 'd7;	// read 8 bits
		      state <= READ_DATA;
		   end
		end
	     end // case: READ_DEVICE_ID_CMD


	     READ_MANUFACTURING_ID_CMD: begin	// send command 0x90
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   nss_reg <= 1'b0;
		   if (cnt < 6'd8) begin
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < 6'd32) begin
		      mosi_reg <= 1'b0;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd32) begin
		      cnt <= 6'd0;
		      readcnt <= 'd7;	// read 8 bits
		      state <= READ_DATA;
		   end
		end
	     end
	     
	     READ_UNIQUE_ID_CMD: begin	// send command 0x4B
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   nss_reg <= 1'b0;
		   if (cnt < 6'd8) begin
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < 6'd40) begin
		      mosi_reg <= 1'b0;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd40) begin
		      cnt <= 6'd0;
		      readcnt <= 'd63;	// read 64 bits
		      state <= READ_DATA;
		   end
		end
	     end
	     
	     READ_JEDEC_ID_CMD: begin	// send command 0x9F
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   nss_reg <= 1'b0;
		   if (cnt < 6'd8) begin
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd8) begin
		      cnt <= 6'd0;
		      readcnt <= 'd23;	// read 24 bits (3 bytes)
		      state <= READ_DATA;
		   end
		end
	     end
	     
	     READ_REG_CMD: begin	// send read status register command 0x05, 0x35, 0x15 (data_in)
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   nss_reg <= 1'b0;
		   if (cnt < 6'd8) begin
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd8) begin
		      cnt <= 6'd0;
		      readcnt <= 'd7;	// read 8 bits
		      state <= READ_DATA;
		   end
		end
	     end
	     
	     RESET_CMD: begin
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];	
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < 6'd11) begin
		      nss_reg <= 1'b1;
		      mosi_reg <= 1'b0;
		      cmd_in <= RESETDATA2;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < 6'd19) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd19) begin
		      nss_reg <= 1'b1;
		      mosi_reg <= 1'b1;
		      cnt <= 6'd0;
		      busy_reg <= 1'b0;
		      data_valid_reg <= 1'd1;
		      state <= IDLE;
		   end
		end
	     end
	     
	     WRITE_ENABLE_CMD: begin	                           // enable page program, erase command
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd8) begin
		      data_valid_reg <= 1'd1;
		      cnt <= 6'd0;
		     
		      if (state_loop_flag)
			state <= WBURST_PAGE;
		      else 
			state <= IDLE;
		      
		   end
		   
		end
	     end // case: WRITE_ENABLE_CMD

	     WRITE_ENABLE_VOL_CMD: begin	// enable page program, erase command
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd8) begin
		      data_valid_reg <= 1'd1;
		      cnt <= 6'd0;
		      state <= IDLE;
		   end
		   
		end
	     end // case: WRITE_ENABLE_CMD


	     WRITE_DISABLE_CMD: begin	// enable page program, erase command
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd8) begin
		      data_valid_reg <= 1'd1;
		      cnt <= 6'd0;
		      state <= IDLE;
		   end
		   
		end
	     end // case: WRITE_ENABLE_CMD
	     
	     
	     
	     WRITE_REG_CMD: begin	// write status register command
		if (sclk_reg_neg) begin  // negedge of sclkreg	
		   nss_reg <= 1'b0;
		   busy_reg <= 1'b1;
		   if (cnt < 6'd8) begin
		      cnt <= cnt + 1'b1;
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		   end
		   else if (cnt == 6'd8) begin
		      writecnt <= 'd1;	// write 1 byte
		      cnt <= 6'd0;
		      mosi_reg <= data_in_reg[7];
		      data_in_reg <= {data_in_reg[6:0], 1'b0};
		      state <= WRITE_DATA;
		   end
		end
	     end
	     
	     WRITE_CMD: begin	// page program
		wfifo_rd0 <= 1'b0;

		
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   //cmd phase
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= cmd_in[7];
		      cmd_in <= {cmd_in[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   
		   //address phase
		   else if (cnt < (6'd8 + ADDR_WIDTH)) begin
		      nss_reg <= 1'b0;
		      cnt <= cnt + 1'b1;
		      mosi_reg <= address_reg[ADDR_WIDTH-1];
		      address_reg <= {address_reg[ADDR_WIDTH-2:0], 1'b0};
		   end
		   
		   //data phase. 
		   else if (cnt == (6'd8 + ADDR_WIDTH)) begin
		      writecnt <= w_burst_cnt_max;	// write 256 bytes //write 3 bytes
		      mosi_reg <= burst_data_wr[7];
		      data_in_reg <= {burst_data_wr[6:0], 1'b0};
		      cnt <= 6'd0;
		      state <= WRITE_DATA;
		   end
		end
	     end
	     
	     ERASE_CMD: begin	// data_in with instruction for sector erase 0x20 or block erase 0x52/0xD8
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (cnt < 6'd8) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= spi_cmd_instr_reg[7];
		      spi_cmd_instr_reg <= {spi_cmd_instr_reg[6:0], 1'b0};
		      busy_reg <= 1'b1;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt < cnt_max) begin
		      nss_reg <= 1'b0;
		      mosi_reg <= address_reg[ADDR_WIDTH-1];
		      address_reg <= {address_reg[ADDR_WIDTH-2:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == cnt_max) begin
		      nss_reg <= 1'b1;
		      mosi_reg <= 1'b1;
		      cnt <= 6'd0;
		      busy_reg <= 1'b0;
		      data_valid_reg <= 1'd1;
		      state <= IDLE;
		   end
		end
	     end
	     
	     WRITE_DATA: begin	// data valid goes 1 when 1 byte has been written on SPI data bus
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   mosi_reg <= data_in_reg[7];
		   if (cnt < 6'd6) begin
		      nss_reg <= 1'b0;
		      data_valid_reg <= 1'b0;
		      data_in_reg <= {data_in_reg[6:0], 1'b0};
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd6) begin
		      data_in_reg <= (burst_wr_flag)? burst_data_wr : data_in;
		      cnt <= cnt + 1'b1;
		   end
		   else if (cnt == 6'd7) begin
		      writecnt <= writecnt - 1'b1;
		      data_in_reg <= {data_in_reg[6:0], 1'b0};
		      data_valid_reg <= 1'b1;
		      cnt <= 6'd0;
		   end

		   if (burst_wr_flag & cnt == 6'd5 & writecnt != 'd1)
		     wfifo_rd0 <= 1'b1;
		   else
		     wfifo_rd0 <= 1'b0;
		   
		   
	
		   if ((writecnt == 'd1) && (cnt == 6'd7)) begin
		      nss_reg <= 1'b1;
		      busy_reg <= 1'b0;
		      //state <= IDLE;
		      burst_wr_flag <= 1'b0; //clear the burst write flag.
		      if (spi_wr_burst_size_cnt == 8'h1)
			begin 
			   page_program_done <= (burst_wr_flag) ? 1'b1 : 1'b0;
			   state <= IDLE;
			   state_loop_flag <= 1'b0;
			end
		      else
			begin
			   page_program_done <= 1'b0;
			   state <= WBURST_CHK_STATUS;
			end
		         
		   end // if ((writecnt == 'd1) && (cnt == 6'd7))
		   
		end
	     end

	     WBURST_CHK_STATUS : begin
		nss_reg <= 1'b1;
		mosi_reg <= 1'b1;
		readcnt <= 'b0;
		read_done <= 1'b0;
		cnt <= 6'd0;

		
		data_out_reg_d <= 8'd0;
		busy_reg <= 1'b1;
		cmd_in <= RDREG1_DATA;
		state_loop_flag <= 1'b1;
		instr_wait_cnt <= instr_wait_cnt + 1;
		if (instr_wait_cnt == 4'hf)
		  begin
		     state <= READ_REG_CMD;
		     instr_wait_cnt <= 4'h0;
		  end
		
	     end // case: WBURST_CHK_STATUS

	     WBURST_WEL_CLR : begin
		if (data_out_reg_d[1] == 1'b0) //WEL flag is clear.*TO FIX - 
		  state <= WBURST_WE_INSTR;
		else //Repeat again if not clear.
		  state <= WBURST_CHK_STATUS;
	     end

	     WBURST_WE_INSTR : begin
		nss_reg <= 1'b1;
		mosi_reg <= 1'b1;
		readcnt <= 'b0;
		read_done <= 1'b0;
		cnt <= 6'd0;
		
		busy_reg <= 1'b1;
		cmd_in <= WRITE_ENABLE_DATA;
		erase_en <= 1'b1;
		instr_wait_cnt <= instr_wait_cnt + 1;

		if (instr_wait_cnt == 4'hf)
		  begin
		     state <= WRITE_ENABLE_CMD;
		     instr_wait_cnt <= 4'h0;
		  end
	     end // case: WBURST_WE_INSTR

	     WBURST_PAGE : begin
		nss_reg <= 1'b1;
		mosi_reg <= 1'b1;
		readcnt <= 'b0;
		read_done <= 1'b0;
		cnt <= 6'd0;
		
		
		busy_reg <= 1'b1;
		cmd_in <= WRITEDATA;
		wfifo_rd0 <= 1'b1;
		burst_wr_flag <= 1'b1;
	
	
		instr_wait_cnt <= instr_wait_cnt + 1;

		if (instr_wait_cnt == 4'hf)
		  begin
		     flash_address_reg <= flash_address_reg + 256;
		     spi_wr_burst_size_cnt <= spi_wr_burst_size_cnt-1; 
		     state <= WRITE_CMD;
		     address_reg <= flash_address_reg + 256;
		     instr_wait_cnt <= 4'h0;
		  end
	     end
	     
	     READ_DUMMY_DATA: begin
		if (sclk_reg_pos) begin  // posedge of sclkreg
		   data_out_reg <= {data_out_reg[6:0], miso};
		   readcnt <= readcnt - 1'b1;
		   if (cnt < 6'd7) begin
		      data_valid_reg <= 1'b0;
		      cnt <= cnt + 1'b1;
		   end
		   else begin
		      data_out_reg_d <= {data_out_reg[6:0], miso};
		      data_valid_reg <= 1'b1;
		      cnt <= 6'd0;
		      if (readcnt == 'd0) begin
			 read_done <= 1'b1;
		      end
		   end
		end
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (read_done == 1'b1) begin
		      burst_rd_flag <= 1'b1;            // set the burst read flag.
		      state <= READ_DATA;
		      read_done <= 1'b0;
		      readcnt <= r_burst_cnt_max;	// read 256 bytes (2048 bits) data
		   end
		end
	     end // case: READ_DUMMY_DATA
	     
	     READ_DATA: begin
		if (sclk_reg_pos) begin  // posedge of sclkreg

		   if (spi_cmd == 5'd21) //Fast Read Dual
		     begin
			data_out_reg <= {data_out_reg[5:0], miso, miso_1};
			readcnt <= readcnt - 1'b1;
		     end   
		   else
		     begin 
			data_out_reg <= {data_out_reg[6:0], miso};
			readcnt <= readcnt - 1'b1;
		     end
		   
		   if ( (spi_cmd == 5'd21 && cnt < 6'd3) || (spi_cmd != 5'd21 && cnt < 6'd7) ) begin
		      data_valid_reg <= 1'b0;
		      cnt <= cnt + 1'b1;
		   end
		   else begin
		      if (spi_cmd == 5'd21) 
			data_out_reg_d <= {data_out_reg[5:0], miso, miso_1};
		      else
			data_out_reg_d <= {data_out_reg[6:0], miso};
		      
		      data_valid_reg <= 1'b1;
		      cnt <= 6'd0;
		      if ((readcnt == 'd0 && (spi_cmd !=5'd20 | spi_cmd !=5'd21 ) ) | ( (spi_cmd == 5'd20 | spi_cmd == 5'd21) & ~rden)) begin
			 read_done <= 1'b1;
		      end
		   end
		end
		if (sclk_reg_neg) begin  // negedge of sclkreg
		   if (read_done == 1'b1) begin
		    
		      burst_rd_flag <= 1'b0; //clear the burst read flag.
		      
		      if (state_loop_flag)
			state <= WBURST_WEL_CLR;
		      else 
			state <= IDLE;
		      
		   end
		end
	     end
	     
	     default: begin
		state <= IDLE;
	     end
	   endcase
	end
     end // always @ (posedge clkin)
   
  

   //To update those read register from SPI Flash.
   always @(posedge clkin)
     if (~nrst)
       begin
	  data_valid_reg0 <= 1'b0;
	  data_valid_reg1 <= 1'b0;
	  wfifo_rd1 <= 1'b0;
       end
     else
       begin
	  data_valid_reg0 <= data_valid_reg;
	  data_valid_reg1 <= data_valid_reg0;
	  wfifo_rd1 <= wfifo_rd0;
	  
       end
   assign wfifo_rd = wfifo_rd0 && ~wfifo_rd1;
   
   
   always @(posedge clkin)
     if (!nrst) begin 
	manufacturing_id_reg <= 8'h0;
	jedec_id_reg <= 24'h0;
	unique_id_reg <= 64'h0;
	device_id_reg <= 8'h0;
	spi_flash_rd_status_reg0 <= 8'h0;
	spi_flash_rd_status_reg1 <= 8'h0;
	spi_flash_rd_status_reg2 <= 8'h0;

	
     end
   
     else if (data_valid_reg0 & ~data_valid_reg1) begin 
       case (spi_cmd)
	 5'd6  : jedec_id_reg[23:0]        <= {data_out_reg_d,jedec_id_reg[23:8]};
	 5'd17 : manufacturing_id_reg[7:0] <= data_out_reg_d;
	 5'd4  : unique_id_reg[63:0]       <= {unique_id_reg[55:0],data_out_reg_d};
	 5'd3  : device_id_reg             <= data_out_reg_d;
	 5'd14 : spi_flash_rd_status_reg0  <= data_out_reg_d;
	 5'd15 : spi_flash_rd_status_reg1  <= data_out_reg_d;
	 5'd16 : spi_flash_rd_status_reg2  <= data_out_reg_d;
	 default : ;
       endcase // case (spi_cmd)
     end
   
   
	 
	  
endmodule // efx_spi_shifter


//////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2013-2019 Efinix Inc. All rights reserved.
//
// This   document  contains  proprietary information  which   is
// protected by  copyright. All rights  are reserved.  This notice
// refers to original work by Efinix, Inc. which may be derivitive
// of other work distributed under license of the authors.  In the
// case of derivative work, nothing in this notice overrides the
// original author's license agreement.  Where applicable, the 
// original license agreement is included in it's original 
// unmodified form immediately below this header.
//
// WARRANTY DISCLAIMER.  
//     THE  DESIGN, CODE, OR INFORMATION ARE PROVIDED “AS IS” AND 
//     EFINIX MAKES NO WARRANTIES, EXPRESS OR IMPLIED WITH 
//     RESPECT THERETO, AND EXPRESSLY DISCLAIMS ANY IMPLIED WARRANTIES, 
//     INCLUDING, WITHOUT LIMITATION, THE IMPLIED WARRANTIES OF 
//     MERCHANTABILITY, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR 
//     PURPOSE.  SOME STATES DO NOT ALLOW EXCLUSIONS OF AN IMPLIED 
//     WARRANTY, SO THIS DISCLAIMER MAY NOT APPLY TO LICENSEE.
//
// LIMITATION OF LIABILITY.  
//     NOTWITHSTANDING ANYTHING TO THE CONTRARY, EXCEPT FOR BODILY 
//     INJURY, EFINIX SHALL NOT BE LIABLE WITH RESPECT TO ANY SUBJECT 
//     MATTER OF THIS AGREEMENT UNDER TORT, CONTRACT, STRICT LIABILITY 
//     OR ANY OTHER LEGAL OR EQUITABLE THEORY (I) FOR ANY INDIRECT, 
//     SPECIAL, INCIDENTAL, EXEMPLARY OR CONSEQUENTIAL DAMAGES OF ANY 
//     CHARACTER INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF 
//     GOODWILL, DATA OR PROFIT, WORK STOPPAGE, OR COMPUTER FAILURE OR 
//     MALFUNCTION, OR IN ANY EVENT (II) FOR ANY AMOUNT IN EXCESS, IN 
//     THE AGGREGATE, OF THE FEE PAID BY LICENSEE TO EFINIX HEREUNDER 
//     (OR, IF THE FEE HAS BEEN WAIVED, $100), EVEN IF EFINIX SHALL HAVE 
//     BEEN INFORMED OF THE POSSIBILITY OF SUCH DAMAGES.  SOME STATES DO 
//     NOT ALLOW THE EXCLUSION OR LIMITATION OF INCIDENTAL OR 
//     CONSEQUENTIAL DAMAGES, SO THIS LIMITATION AND EXCLUSION MAY NOT 
//     APPLY TO LICENSEE.
//
/////////////////////////////////////////////////////////////////////////////

