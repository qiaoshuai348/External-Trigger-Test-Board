`timescale 1ns/1ps

module tb_protocol;
    localparam CLK_HZ = 10000000;
    localparam BAUD = 500000;
    localparam BIT_NS = 2000;
    reg clk = 0;
    reg rst_n = 0;
    reg uart_rx_pin = 1;
    wire uart_tx_pin;
    wire [7:0] monitor_data;
    wire monitor_valid;
    wire monitor_error;
    wire trigger_out_pin;
    wire trigger_in_pin = trigger_out_pin;
    reg [7:0] response_data [0:63];
    integer response_len;
    integer errors = 0;
    integer i;
    reg [15:0] crc_work;

    always #50 clk = ~clk;

    external_trigger_controller #(.CLK_HZ(CLK_HZ), .BAUD(BAUD)) dut(
        .clk(clk), .rst_n(rst_n), .uart_rx_pin(uart_rx_pin),
        .uart_tx_pin(uart_tx_pin), .trigger_in_pin(trigger_in_pin),
        .trigger_out_pin(trigger_out_pin)
    );
    uart_rx #(.CLK_HZ(CLK_HZ), .BAUD(BAUD)) response_monitor(
        .clk(clk), .rst_n(rst_n), .rx(uart_tx_pin),
        .data_out(monitor_data), .data_valid(monitor_valid), .frame_error(monitor_error)
    );

    function [15:0] crc16_byte;
        input [15:0] crc_in;
        input [7:0] data_in;
        integer k;
        reg [15:0] c;
        begin
            c = crc_in ^ {data_in, 8'h00};
            for (k=0; k<8; k=k+1)
                c = c[15] ? ((c << 1) ^ 16'h1021) : (c << 1);
            crc16_byte = c;
        end
    endfunction

    task send_byte;
        input [7:0] value;
        integer k;
        begin
            uart_rx_pin = 0; #(BIT_NS);
            for (k=0; k<8; k=k+1) begin
                uart_rx_pin = value[k]; #(BIT_NS);
            end
            uart_rx_pin = 1; #(BIT_NS);
        end
    endtask

    task recv_byte;
        output [7:0] value;
        begin
            @(posedge monitor_valid);
            value = monitor_data;
        end
    endtask

    task send_cmd0;
        input [7:0] cmd;
        begin
            crc_work = 16'hffff;
            send_byte(8'h55); send_byte(8'haa);
            send_byte(8'h01); crc_work = crc16_byte(crc_work, 8'h01);
            send_byte(cmd); crc_work = crc16_byte(crc_work, cmd);
            send_byte(8'd0); crc_work = crc16_byte(crc_work, 8'd0);
            send_byte(crc_work[7:0]); send_byte(crc_work[15:8]);
        end
    endtask

    task send_cmd1;
        input [7:0] cmd;
        input [7:0] p0;
        begin
            crc_work = 16'hffff;
            send_byte(8'h55); send_byte(8'haa);
            send_byte(8'h01); crc_work = crc16_byte(crc_work, 8'h01);
            send_byte(cmd); crc_work = crc16_byte(crc_work, cmd);
            send_byte(8'd1); crc_work = crc16_byte(crc_work, 8'd1);
            send_byte(p0); crc_work = crc16_byte(crc_work, p0);
            send_byte(crc_work[7:0]); send_byte(crc_work[15:8]);
        end
    endtask

    task send_cmd4;
        input [7:0] cmd;
        input [31:0] value;
        integer k;
        reg [7:0] b;
        begin
            crc_work = 16'hffff;
            send_byte(8'h55); send_byte(8'haa);
            send_byte(8'h01); crc_work = crc16_byte(crc_work, 8'h01);
            send_byte(cmd); crc_work = crc16_byte(crc_work, cmd);
            send_byte(8'd4); crc_work = crc16_byte(crc_work, 8'd4);
            for (k=0; k<4; k=k+1) begin
                b = value >> (8*k); send_byte(b); crc_work = crc16_byte(crc_work, b);
            end
            send_byte(crc_work[7:0]); send_byte(crc_work[15:8]);
        end
    endtask

    task receive_response;
        input [7:0] expected_cmd;
        input [7:0] expected_status;
        reg [7:0] b;
        reg [7:0] crc_lo;
        integer k;
        begin
            recv_byte(b); if (b != 8'h55) begin $display("FAIL response SOF1 %02x", b); errors=errors+1; end
            recv_byte(b); if (b != 8'haa) begin $display("FAIL response SOF2 %02x", b); errors=errors+1; end
            crc_work = 16'hffff;
            recv_byte(b); crc_work=crc16_byte(crc_work,b); if (b != 1) errors=errors+1;
            recv_byte(b); crc_work=crc16_byte(crc_work,b); if (b != (expected_cmd|8'h80)) begin $display("FAIL response cmd %02x",b); errors=errors+1; end
            recv_byte(b); crc_work=crc16_byte(crc_work,b); response_len=b;
            for (k=0; k<response_len; k=k+1) begin
                recv_byte(response_data[k]); crc_work=crc16_byte(crc_work,response_data[k]);
            end
            recv_byte(crc_lo); recv_byte(b);
            if ({b,crc_lo} != crc_work) begin $display("FAIL response CRC"); errors=errors+1; end
            if (response_data[0] != expected_status) begin
                $display("FAIL response status=%0d expected=%0d",response_data[0],expected_status); errors=errors+1;
            end
        end
    endtask

    initial begin
        #(BIT_NS*3); rst_n=1; #(BIT_NS*3);

        send_cmd0(8'h01); $display("INFO sent PING at %0t", $time); receive_response(8'h01, 0);
        if (response_len != 13) begin $display("FAIL ping length"); errors=errors+1; end

        send_cmd4(8'h13, 1); receive_response(8'h13, 6);
        send_cmd4(8'h10, 32'd40); receive_response(8'h10, 0);
        send_cmd4(8'h11, 32'd10); receive_response(8'h11, 0);
        send_cmd1(8'h12, 8'h00); receive_response(8'h12, 0);
        send_cmd0(8'h22); receive_response(8'h22, 0);
        send_cmd4(8'h13, 32'd3); receive_response(8'h13, 0);
        wait (!dut.gen_running);
        #(BIT_NS*2);
        send_cmd0(8'h21); receive_response(8'h21, 0);
        if ({response_data[4],response_data[3],response_data[2],response_data[1]} != 3) begin
            $display("FAIL looped event count=%0d", {response_data[4],response_data[3],response_data[2],response_data[1]}); errors=errors+1;
        end

        send_cmd4(8'h11, 32'd40); receive_response(8'h11, 5);
        send_cmd0(8'h20); receive_response(8'h20, 0);
        if ({response_data[10],response_data[9],response_data[8],response_data[7]} != 10) begin
            $display("FAIL invalid width changed active configuration"); errors=errors+1;
        end

        send_cmd1(8'h12, 8'h01); receive_response(8'h12, 0);
        send_cmd0(8'h20); receive_response(8'h20, 0);
        if (!response_data[11][0]) begin
            $display("FAIL output polarity was not applied"); errors=errors+1;
        end
        send_cmd1(8'h12, 8'h00); receive_response(8'h12, 0);

        // Bad CRC must produce status 4 and parser must recover for the next PING.
        send_byte(8'h55); send_byte(8'haa); send_byte(8'h01); send_byte(8'h01);
        send_byte(8'h00); send_byte(8'h00); send_byte(8'h00);
        receive_response(8'h01, 4);
        send_cmd0(8'h01); receive_response(8'h01, 0);

        if (errors == 0) begin
            $display("PASS tb_protocol");
            $finish;
        end else begin
            $display("FAIL tb_protocol errors=%0d", errors);
            $fatal(1);
        end
    end

    initial begin
        #(BIT_NS*10000);
        $display("FAIL tb_protocol watchdog state=%0d rx_valid=%b response_start=%b response_busy=%b", dut.parser_state, dut.rx_valid, dut.response_start, dut.response_busy);
        $fatal(1);
    end
endmodule
