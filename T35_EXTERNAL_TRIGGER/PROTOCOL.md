# T35 外触发 UART 协议 v1

串口为 115200 baud、8N1。所有多字节整数使用小端序，时间单位均为 10 ns tick。

## 帧

```text
55 AA | VERSION | CMD | LENGTH | PAYLOAD | CRC_LO CRC_HI
```

- `VERSION` 固定为 `0x01`，`LENGTH` 最大 64。
- CRC-16/CCITT-FALSE：poly `0x1021`、init `0xFFFF`、refin/refout false、xorout `0x0000`，覆盖 VERSION、CMD、LENGTH 和 PAYLOAD。
- 响应 CMD 为请求 CMD `| 0x80`；响应 payload 首字节始终为状态码。
- 帧内相邻字节超过 20 ms 时解析器复位并报告帧超时。

状态码：`0=OK`、`1=BAD_VERSION`、`2=UNKNOWN_CMD`、`3=BAD_LENGTH`、`4=BAD_CRC`、`5=INVALID_PARAMETER`、`6=NOT_CONFIGURED`、`7=BUSY`、`8=FRAME_TIMEOUT`、`9=UART_FRAME_ERROR`。

## 命令

| CMD | 名称 | 请求 payload |
|---:|---|---|
| `01` | PING | 空 |
| `10` | SET_PERIOD | `u32 period_ticks` |
| `11` | SET_WIDTH | `u32 width_ticks` |
| `12` | SET_POLARITY | `u8`：bit0 输出低有效，bit1 输入低有效；其余位必须为 0 |
| `13` | START | `u32 count`，0 为连续 |
| `14` | STOP | 空 |
| `15` | PULSE_ONCE | 空 |
| `20` | READ_STATUS | 空 |
| `21` | READ_INPUT_STATS | 空 |
| `22` | CLEAR_STATS | 空 |
| `30` | LOOPBACK_TEST | `u32 period, u32 width, u32 count, u8 polarity` |

复位后必须分别成功设置 period 和 width；只有 `0 < width < period` 才会提交配置。运行中更新在周期边界原子生效。`LOOPBACK_TEST` 要求 `count>0` 且 polarity 为 `0x00`。

## 成功响应数据

以下结构不再重复开头的状态字节。

- PING：`fw_major, fw_minor, fw_patch, protocol, u32 clock_hz, u32 capabilities`。
- READ_STATUS：`u16 flags, u32 period, u32 width, u8 polarity, u32 remaining, u8 last_error`。
- READ_INPUT_STATS：`u32 events, u32 last_width, u32 last_period, u32 too_narrow, u16 flags`。

READ_STATUS flags：bit0 running、bit1 configured、bit2 pending update、bit3 low-active precharge、bit4 loopback busy、bit5 loopback pass、bit6 loopback fail、bit7 input timeout、bit8 counter overflow。

READ_INPUT_STATS flags：bit0 timeout、bit1 overflow。计数器饱和在 `0xFFFFFFFF`；同步后宽度小于 2 tick 会增加 too-narrow 计数。
