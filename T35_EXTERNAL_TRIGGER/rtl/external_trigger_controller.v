`timescale 1ns/1ps

module external_trigger_controller #(
    parameter CLK_HZ = 100000000,
    parameter BAUD   = 115200
)(
    input  wire clk,
    input  wire rst_n,
    input  wire uart_rx_pin,
    output wire uart_tx_pin,
    input  wire trigger_in_pin,
    output wire trigger_out_pin
);
    localparam VERSION = 8'h01;
    localparam MAX_PAYLOAD = 64;
    localparam INTERBYTE_TIMEOUT = 2000000;

    localparam ST_OK             = 8'd0;
    localparam ST_BAD_VERSION    = 8'd1;
    localparam ST_UNKNOWN_CMD    = 8'd2;
    localparam ST_BAD_LENGTH     = 8'd3;
    localparam ST_BAD_CRC        = 8'd4;
    localparam ST_INVALID_PARAM  = 8'd5;
    localparam ST_NOT_CONFIGURED = 8'd6;
    localparam ST_BUSY           = 8'd7;
    localparam ST_FRAME_TIMEOUT  = 8'd8;
    localparam ST_UART_ERROR     = 8'd9;

    localparam CMD_PING        = 8'h01;
    localparam CMD_SET_PERIOD  = 8'h10;
    localparam CMD_SET_WIDTH   = 8'h11;
    localparam CMD_SET_POL     = 8'h12;
    localparam CMD_START       = 8'h13;
    localparam CMD_STOP        = 8'h14;
    localparam CMD_PULSE_ONCE  = 8'h15;
    localparam CMD_STATUS      = 8'h20;
    localparam CMD_STATS       = 8'h21;
    localparam CMD_CLEAR       = 8'h22;
    localparam CMD_LOOPBACK    = 8'h30;

    wire [7:0] rx_byte;
    wire       rx_valid;
    wire       rx_frame_error;
    reg  [7:0] tx_byte;
    reg        tx_valid;
    wire       tx_ready;
    wire       tx_busy;

    uart_rx #(.CLK_HZ(CLK_HZ), .BAUD(BAUD)) uart_rx_inst (
        .clk(clk), .rst_n(rst_n), .rx(uart_rx_pin),
        .data_out(rx_byte), .data_valid(rx_valid), .frame_error(rx_frame_error)
    );
    uart_tx #(.CLK_HZ(CLK_HZ), .BAUD(BAUD)) uart_tx_inst (
        .clk(clk), .rst_n(rst_n), .data_in(tx_byte), .data_valid(tx_valid),
        .data_ready(tx_ready), .tx(uart_tx_pin), .busy(tx_busy)
    );

    reg        cfg_apply;
    reg        cfg_apply_pending;
    reg [31:0] cfg_period;
    reg [31:0] cfg_width;
    reg        cfg_output_active_low;
    reg        gen_start;
    reg [31:0] gen_start_count;
    reg        gen_stop;
    wire       gen_running;
    wire       gen_precharge;
    wire       gen_pending;
    wire [31:0] active_period;
    wire [31:0] active_width;
    wire       active_output_low;
    wire [31:0] gen_remaining;
    wire       cycle_boundary;

    trigger_generator trigger_generator_inst (
        .clk(clk), .rst_n(rst_n),
        .cfg_apply(cfg_apply), .cfg_period(cfg_period), .cfg_width(cfg_width),
        .cfg_active_low(cfg_output_active_low),
        .start(gen_start), .start_count(gen_start_count), .stop(gen_stop),
        .trigger_out(trigger_out_pin), .running(gen_running),
        .precharge(gen_precharge), .pending_update(gen_pending),
        .active_period(active_period), .active_width(active_width),
        .active_low(active_output_low), .remaining(gen_remaining),
        .cycle_boundary(cycle_boundary)
    );

    reg input_active_low;
    reg clear_stats;
    wire [31:0] event_count;
    wire [31:0] last_width;
    wire [31:0] last_period;
    wire [31:0] too_narrow_count;
    wire timeout_flag;
    wire overflow_flag;
    wire capture_event;
    wire [31:0] twice_period = active_period[31] ? 32'hffffffff : (active_period << 1);
    wire [31:0] timeout_ticks = (twice_period < 32'd1000000) ? 32'd1000000 : twice_period;

    trigger_capture trigger_capture_inst (
        .clk(clk), .rst_n(rst_n), .async_in(trigger_in_pin),
        .active_low(input_active_low), .monitor_enable(gen_running),
        .timeout_ticks(timeout_ticks), .clear_stats(clear_stats),
        .event_count(event_count), .last_width(last_width),
        .last_period(last_period), .too_narrow_count(too_narrow_count),
        .timeout_flag(timeout_flag), .overflow_flag(overflow_flag),
        .event_pulse(capture_event)
    );

    function [15:0] crc16_byte;
        input [15:0] crc_in;
        input [7:0] data_in;
        integer i;
        reg [15:0] c;
        begin
            c = crc_in ^ {data_in, 8'h00};
            for (i = 0; i < 8; i = i + 1)
                c = c[15] ? ((c << 1) ^ 16'h1021) : (c << 1);
            crc16_byte = c;
        end
    endfunction

    reg [7:0] rx_payload [0:MAX_PAYLOAD-1];
    // Longest response is READ_INPUT_STATS (19 bytes including status).
    reg [7:0] resp_payload [0:18];
    reg [7:0] parser_state;
    reg [7:0] request_version;
    reg [7:0] request_cmd;
    reg [7:0] request_length;
    reg [7:0] payload_index;
    reg [15:0] request_crc;
    reg [7:0] received_crc_lo;
    reg [31:0] timeout_count;
    reg        execute_request;
    reg        parser_error_valid;
    reg [7:0]  parser_error_status;
    reg [7:0]  parser_error_cmd;

    localparam P_SOF1    = 8'd0;
    localparam P_SOF2    = 8'd1;
    localparam P_VERSION = 8'd2;
    localparam P_CMD     = 8'd3;
    localparam P_LENGTH  = 8'd4;
    localparam P_PAYLOAD = 8'd5;
    localparam P_CRC_LO  = 8'd6;
    localparam P_CRC_HI  = 8'd7;

    reg        response_start;
    reg [7:0]  response_cmd;
    reg [7:0]  response_length;
    reg        response_busy;
    reg [3:0]  response_state;
    reg [7:0]  response_index;
    reg [15:0] response_crc;

    localparam R_SOF1    = 4'd0;
    localparam R_SOF2    = 4'd1;
    localparam R_VERSION = 4'd2;
    localparam R_CMD     = 4'd3;
    localparam R_LENGTH  = 4'd4;
    localparam R_PAYLOAD = 4'd5;
    localparam R_CRC_LO  = 4'd6;
    localparam R_CRC_HI  = 4'd7;

    task queue_status;
        input [7:0] cmd_value;
        input [7:0] status_value;
        begin
            response_cmd        <= cmd_value | 8'h80;
            response_length     <= 8'd1;
            resp_payload[0]     <= status_value;
            response_start      <= 1'b1;
        end
    endtask

    // Request parser. The host is request/response serialized; while a response is
    // being sent, a new request is deliberately ignored.
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            parser_state    <= P_SOF1;
            request_version <= 8'd0;
            request_cmd     <= 8'd0;
            request_length  <= 8'd0;
            payload_index   <= 8'd0;
            request_crc     <= 16'hffff;
            received_crc_lo <= 8'd0;
            timeout_count   <= 32'd0;
            execute_request <= 1'b0;
            parser_error_valid  <= 1'b0;
            parser_error_status <= ST_OK;
            parser_error_cmd    <= 8'd0;
        end else begin
            execute_request <= 1'b0;
            parser_error_valid <= 1'b0;

            if (rx_frame_error) begin
                parser_state  <= P_SOF1;
                timeout_count <= 32'd0;
                if (!response_busy) begin
                    parser_error_valid  <= 1'b1;
                    parser_error_status <= ST_UART_ERROR;
                    parser_error_cmd    <= request_cmd;
                end
            end else if (rx_valid && !response_busy) begin
                timeout_count <= 32'd0;
                case (parser_state)
                    P_SOF1: begin
                        if (rx_byte == 8'h55)
                            parser_state <= P_SOF2;
                    end
                    P_SOF2: begin
                        if (rx_byte == 8'haa)
                            parser_state <= P_VERSION;
                        else if (rx_byte != 8'h55)
                            parser_state <= P_SOF1;
                    end
                    P_VERSION: begin
                        request_version <= rx_byte;
                        request_crc     <= crc16_byte(16'hffff, rx_byte);
                        parser_state    <= P_CMD;
                    end
                    P_CMD: begin
                        request_cmd  <= rx_byte;
                        request_crc  <= crc16_byte(request_crc, rx_byte);
                        parser_state <= P_LENGTH;
                    end
                    P_LENGTH: begin
                        request_length <= rx_byte;
                        request_crc    <= crc16_byte(request_crc, rx_byte);
                        payload_index  <= 8'd0;
                        if (rx_byte > MAX_PAYLOAD) begin
                            parser_state <= P_SOF1;
                            parser_error_valid  <= 1'b1;
                            parser_error_status <= ST_BAD_LENGTH;
                            parser_error_cmd    <= request_cmd;
                        end else if (rx_byte == 0) begin
                            parser_state <= P_CRC_LO;
                        end else begin
                            parser_state <= P_PAYLOAD;
                        end
                    end
                    P_PAYLOAD: begin
                        rx_payload[payload_index] <= rx_byte;
                        request_crc <= crc16_byte(request_crc, rx_byte);
                        if (payload_index + 1'b1 >= request_length)
                            parser_state <= P_CRC_LO;
                        else
                            payload_index <= payload_index + 1'b1;
                    end
                    P_CRC_LO: begin
                        received_crc_lo <= rx_byte;
                        parser_state    <= P_CRC_HI;
                    end
                    P_CRC_HI: begin
                        parser_state <= P_SOF1;
                        if ({rx_byte, received_crc_lo} == request_crc)
                            execute_request <= 1'b1;
                        else begin
                            parser_error_valid  <= 1'b1;
                            parser_error_status <= ST_BAD_CRC;
                            parser_error_cmd    <= request_cmd;
                        end
                    end
                    default: parser_state <= P_SOF1;
                endcase
            end else if (parser_state != P_SOF1) begin
                if (timeout_count >= INTERBYTE_TIMEOUT-1) begin
                    parser_state  <= P_SOF1;
                    timeout_count <= 32'd0;
                    if (!response_busy) begin
                        parser_error_valid  <= 1'b1;
                        parser_error_status <= ST_FRAME_TIMEOUT;
                        parser_error_cmd    <= request_cmd;
                    end
                end else begin
                    timeout_count <= timeout_count + 1'b1;
                end
            end
        end
    end

    reg [31:0] shadow_period;
    reg [31:0] shadow_width;
    reg        period_received;
    reg        width_received;
    reg        configured;
    reg [7:0]  last_error;
    reg        loopback_busy;
    reg        loopback_pass;
    reg        loopback_fail;
    reg [31:0] loopback_expected;
    reg        loopback_start_pending;
    reg [5:0]  loopback_clear_delay;
    reg        gen_running_d;
    reg [4:0]  loopback_finish_wait;

    wire [31:0] payload_u32_0 = {rx_payload[3], rx_payload[2], rx_payload[1], rx_payload[0]};
    wire [31:0] payload_u32_1 = {rx_payload[7], rx_payload[6], rx_payload[5], rx_payload[4]};
    wire [31:0] payload_u32_2 = {rx_payload[11], rx_payload[10], rx_payload[9], rx_payload[8]};
    wire [15:0] status_flags = {
        7'd0, overflow_flag, timeout_flag, loopback_fail, loopback_pass,
        loopback_busy, gen_precharge, gen_pending, configured, gen_running
    };

    // Command execution and loopback supervision.
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            response_start          <= 1'b0;
            response_cmd            <= 8'd0;
            response_length         <= 8'd0;
            cfg_apply               <= 1'b0;
            cfg_apply_pending       <= 1'b0;
            cfg_period              <= 32'd100000;
            cfg_width               <= 32'd20;
            cfg_output_active_low   <= 1'b0;
            input_active_low        <= 1'b1;
            gen_start               <= 1'b0;
            gen_start_count         <= 32'd0;
            gen_stop                <= 1'b0;
            clear_stats             <= 1'b0;
            shadow_period           <= 32'd100000;
            shadow_width            <= 32'd20;
            period_received         <= 1'b0;
            width_received          <= 1'b0;
            configured             <= 1'b0;
            last_error              <= ST_OK;
            loopback_busy           <= 1'b0;
            loopback_pass           <= 1'b0;
            loopback_fail           <= 1'b0;
            loopback_expected       <= 32'd0;
            loopback_start_pending  <= 1'b0;
            loopback_clear_delay    <= 6'd0;
            gen_running_d           <= 1'b0;
            loopback_finish_wait    <= 5'd0;
        end else begin
            response_start <= 1'b0;
            cfg_apply      <= 1'b0;
            gen_start      <= 1'b0;
            gen_stop       <= 1'b0;
            clear_stats    <= 1'b0;
            gen_running_d  <= gen_running;

            // Configuration values are registered first, then applied one cycle
            // later so the generator never samples the previous polarity/value.
            if (cfg_apply_pending) begin
                cfg_apply         <= 1'b1;
                cfg_apply_pending <= 1'b0;
            end else if (loopback_start_pending) begin
                loopback_start_pending <= 1'b0;
                gen_start              <= 1'b1;
                gen_start_count        <= loopback_expected;
                clear_stats            <= 1'b1;
                loopback_busy          <= 1'b1;
                loopback_clear_delay   <= 6'd0;
            end

            if (loopback_busy && gen_running)
                loopback_clear_delay <= loopback_clear_delay + 1'b1;

            if (loopback_busy && gen_running_d && !gen_running)
                loopback_finish_wait <= 5'd16;
            else if (loopback_finish_wait != 0) begin
                loopback_finish_wait <= loopback_finish_wait - 1'b1;
                if (loopback_finish_wait == 1) begin
                    loopback_busy <= 1'b0;
                    if ((event_count == loopback_expected) &&
                        ((last_width + 1'b1 >= active_width) && (last_width <= active_width + 1'b1)) &&
                        ((loopback_expected < 2) ||
                         ((last_period + 1'b1 >= active_period) && (last_period <= active_period + 1'b1)))) begin
                        loopback_pass <= 1'b1;
                        loopback_fail <= 1'b0;
                    end else begin
                        loopback_pass <= 1'b0;
                        loopback_fail <= 1'b1;
                    end
                end
            end

            if (parser_error_valid && !response_busy && !response_start) begin
                last_error <= parser_error_status;
                queue_status(parser_error_cmd, parser_error_status);
            end else if (execute_request && !response_busy && !response_start) begin
                if (request_version != VERSION) begin
                    last_error <= ST_BAD_VERSION;
                    queue_status(request_cmd, ST_BAD_VERSION);
                end else begin
                    case (request_cmd)
                        CMD_PING: begin
                            if (request_length != 0) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else begin
                                response_cmd    <= request_cmd | 8'h80;
                                response_length <= 8'd13;
                                resp_payload[0] <= ST_OK;
                                resp_payload[1] <= 8'd1;
                                resp_payload[2] <= 8'd0;
                                resp_payload[3] <= 8'd0;
                                resp_payload[4] <= VERSION;
                                resp_payload[5] <= CLK_HZ[7:0];
                                resp_payload[6] <= CLK_HZ[15:8];
                                resp_payload[7] <= CLK_HZ[23:16];
                                resp_payload[8] <= CLK_HZ[31:24];
                                resp_payload[9] <= 8'hff;
                                resp_payload[10]<= 8'h01;
                                resp_payload[11]<= 8'h00;
                                resp_payload[12]<= 8'h00;
                                response_start  <= 1'b1;
                                last_error      <= ST_OK;
                            end
                        end
                        CMD_SET_PERIOD: begin
                            if (request_length != 4) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else if ((payload_u32_0 < 2) ||
                                         (width_received && (payload_u32_0 <= shadow_width))) begin
                                last_error <= ST_INVALID_PARAM;
                                queue_status(request_cmd, ST_INVALID_PARAM);
                            end else begin
                                shadow_period   <= payload_u32_0;
                                period_received <= 1'b1;
                                if (width_received) begin
                                    cfg_period            <= payload_u32_0;
                                    cfg_width             <= shadow_width;
                                    cfg_output_active_low <= cfg_output_active_low;
                                    cfg_apply_pending     <= 1'b1;
                                    configured           <= 1'b1;
                                end
                                last_error <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_SET_WIDTH: begin
                            if (request_length != 4) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else if ((payload_u32_0 == 0) ||
                                         (period_received && (payload_u32_0 >= shadow_period))) begin
                                last_error <= ST_INVALID_PARAM;
                                queue_status(request_cmd, ST_INVALID_PARAM);
                            end else begin
                                shadow_width   <= payload_u32_0;
                                width_received <= 1'b1;
                                if (period_received) begin
                                    cfg_period            <= shadow_period;
                                    cfg_width             <= payload_u32_0;
                                    cfg_output_active_low <= cfg_output_active_low;
                                    cfg_apply_pending     <= 1'b1;
                                    configured           <= 1'b1;
                                end
                                last_error <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_SET_POL: begin
                            if (request_length != 1) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else if (rx_payload[0][7:2] != 0) begin
                                last_error <= ST_INVALID_PARAM;
                                queue_status(request_cmd, ST_INVALID_PARAM);
                            end else begin
                                cfg_output_active_low <= rx_payload[0][0];
                                input_active_low      <= rx_payload[0][1];
                                if (configured) begin
                                    cfg_period <= shadow_period;
                                    cfg_width  <= shadow_width;
                                    cfg_apply_pending <= 1'b1;
                                end
                                last_error <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_START: begin
                            if (request_length != 4) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else if (!configured) begin
                                last_error <= ST_NOT_CONFIGURED;
                                queue_status(request_cmd, ST_NOT_CONFIGURED);
                            end else if (gen_running || loopback_busy) begin
                                last_error <= ST_BUSY;
                                queue_status(request_cmd, ST_BUSY);
                            end else begin
                                gen_start       <= 1'b1;
                                gen_start_count <= payload_u32_0;
                                last_error      <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_STOP: begin
                            if (request_length != 0) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else begin
                                gen_stop      <= 1'b1;
                                loopback_busy <= 1'b0;
                                last_error    <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_PULSE_ONCE: begin
                            if (request_length != 0) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else if (!configured) begin
                                last_error <= ST_NOT_CONFIGURED;
                                queue_status(request_cmd, ST_NOT_CONFIGURED);
                            end else if (gen_running || loopback_busy) begin
                                last_error <= ST_BUSY;
                                queue_status(request_cmd, ST_BUSY);
                            end else begin
                                gen_start       <= 1'b1;
                                gen_start_count <= 32'd1;
                                last_error      <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_STATUS: begin
                            if (request_length != 0) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else begin
                                response_cmd     <= request_cmd | 8'h80;
                                response_length  <= 8'd17;
                                resp_payload[0]  <= ST_OK;
                                resp_payload[1]  <= status_flags[7:0];
                                resp_payload[2]  <= status_flags[15:8];
                                resp_payload[3]  <= active_period[7:0];
                                resp_payload[4]  <= active_period[15:8];
                                resp_payload[5]  <= active_period[23:16];
                                resp_payload[6]  <= active_period[31:24];
                                resp_payload[7]  <= active_width[7:0];
                                resp_payload[8]  <= active_width[15:8];
                                resp_payload[9]  <= active_width[23:16];
                                resp_payload[10] <= active_width[31:24];
                                resp_payload[11] <= {6'd0, input_active_low, active_output_low};
                                resp_payload[12] <= gen_remaining[7:0];
                                resp_payload[13] <= gen_remaining[15:8];
                                resp_payload[14] <= gen_remaining[23:16];
                                resp_payload[15] <= gen_remaining[31:24];
                                resp_payload[16] <= last_error;
                                response_start   <= 1'b1;
                            end
                        end
                        CMD_STATS: begin
                            if (request_length != 0) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else begin
                                response_cmd     <= request_cmd | 8'h80;
                                response_length  <= 8'd19;
                                resp_payload[0]  <= ST_OK;
                                resp_payload[1]  <= event_count[7:0];
                                resp_payload[2]  <= event_count[15:8];
                                resp_payload[3]  <= event_count[23:16];
                                resp_payload[4]  <= event_count[31:24];
                                resp_payload[5]  <= last_width[7:0];
                                resp_payload[6]  <= last_width[15:8];
                                resp_payload[7]  <= last_width[23:16];
                                resp_payload[8]  <= last_width[31:24];
                                resp_payload[9]  <= last_period[7:0];
                                resp_payload[10] <= last_period[15:8];
                                resp_payload[11] <= last_period[23:16];
                                resp_payload[12] <= last_period[31:24];
                                resp_payload[13] <= too_narrow_count[7:0];
                                resp_payload[14] <= too_narrow_count[15:8];
                                resp_payload[15] <= too_narrow_count[23:16];
                                resp_payload[16] <= too_narrow_count[31:24];
                                resp_payload[17] <= {6'd0, overflow_flag, timeout_flag};
                                resp_payload[18] <= 8'd0;
                                response_start   <= 1'b1;
                            end
                        end
                        CMD_CLEAR: begin
                            if (request_length != 0) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else begin
                                clear_stats   <= 1'b1;
                                loopback_pass <= 1'b0;
                                loopback_fail <= 1'b0;
                                last_error    <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        CMD_LOOPBACK: begin
                            if (request_length != 13) begin
                                last_error <= ST_BAD_LENGTH;
                                queue_status(request_cmd, ST_BAD_LENGTH);
                            end else if (gen_running || loopback_busy) begin
                                last_error <= ST_BUSY;
                                queue_status(request_cmd, ST_BUSY);
                            end else if ((payload_u32_0 < 2) || (payload_u32_1 == 0) ||
                                         (payload_u32_1 >= payload_u32_0) || (payload_u32_2 == 0) ||
                                         (rx_payload[12] != 0)) begin
                                // Physical loopback is intentionally high-active. Low-active output
                                // returns to the mandated stopped-low state and would create an extra edge.
                                last_error <= ST_INVALID_PARAM;
                                queue_status(request_cmd, ST_INVALID_PARAM);
                            end else begin
                                shadow_period         <= payload_u32_0;
                                shadow_width          <= payload_u32_1;
                                period_received       <= 1'b1;
                                width_received        <= 1'b1;
                                configured           <= 1'b1;
                                cfg_period            <= payload_u32_0;
                                cfg_width             <= payload_u32_1;
                                cfg_output_active_low <= 1'b0;
                                input_active_low      <= 1'b0;
                                cfg_apply_pending     <= 1'b1;
                                loopback_expected     <= payload_u32_2;
                                loopback_start_pending<= 1'b1;
                                loopback_pass         <= 1'b0;
                                loopback_fail         <= 1'b0;
                                last_error            <= ST_OK;
                                queue_status(request_cmd, ST_OK);
                            end
                        end
                        default: begin
                            last_error <= ST_UNKNOWN_CMD;
                            queue_status(request_cmd, ST_UNKNOWN_CMD);
                        end
                    endcase
                end
            end
        end
    end

    // Response serializer and CRC generator.
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            response_busy  <= 1'b0;
            response_state <= R_SOF1;
            response_index <= 8'd0;
            response_crc   <= 16'hffff;
            tx_byte        <= 8'hff;
            tx_valid       <= 1'b0;
        end else begin
            if (response_start && !response_busy) begin
                response_busy  <= 1'b1;
                response_state <= R_SOF1;
                response_index <= 8'd0;
                response_crc   <= 16'hffff;
                tx_valid       <= 1'b0;
            end else if (response_busy && tx_valid && tx_ready) begin
                // The UART samples the registered valid/data on this edge. Advance
                // only after that acceptance so no byte can be overwritten.
                tx_valid <= 1'b0;
                case (response_state)
                    R_SOF1: response_state <= R_SOF2;
                    R_SOF2: response_state <= R_VERSION;
                    R_VERSION: begin
                        response_crc <= crc16_byte(16'hffff, VERSION);
                        response_state <= R_CMD;
                    end
                    R_CMD: begin
                        response_crc <= crc16_byte(response_crc, response_cmd);
                        response_state <= R_LENGTH;
                    end
                    R_LENGTH: begin
                        response_crc <= crc16_byte(response_crc, response_length);
                        response_index <= 8'd0;
                        response_state <= (response_length == 0) ? R_CRC_LO : R_PAYLOAD;
                    end
                    R_PAYLOAD: begin
                        response_crc <= crc16_byte(response_crc, resp_payload[response_index]);
                        if (response_index + 1'b1 >= response_length)
                            response_state <= R_CRC_LO;
                        else
                            response_index <= response_index + 1'b1;
                    end
                    R_CRC_LO: response_state <= R_CRC_HI;
                    R_CRC_HI: begin
                        response_busy <= 1'b0;
                        response_state <= R_SOF1;
                    end
                    default: begin response_busy <= 1'b0; response_state <= R_SOF1; end
                endcase
            end else if (response_busy && !tx_valid && tx_ready) begin
                case (response_state)
                    R_SOF1:    tx_byte <= 8'h55;
                    R_SOF2:    tx_byte <= 8'haa;
                    R_VERSION: tx_byte <= VERSION;
                    R_CMD:     tx_byte <= response_cmd;
                    R_LENGTH:  tx_byte <= response_length;
                    R_PAYLOAD: tx_byte <= resp_payload[response_index];
                    R_CRC_LO:  tx_byte <= response_crc[7:0];
                    R_CRC_HI:  tx_byte <= response_crc[15:8];
                    default:   tx_byte <= 8'hff;
                endcase
                tx_valid <= 1'b1;
            end
        end
    end
endmodule
