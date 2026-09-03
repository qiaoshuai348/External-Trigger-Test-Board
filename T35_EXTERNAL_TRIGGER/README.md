# T35 P3 外触发控制器

这是从原始 `T35_FRAMEWORK` 派生的独立最小工程。原工程、原 bitstream、DDR3、MIPI、FX3、Flash、I2C、GPS、P5 光耦通道均未被修改或实例化。

## 固定硬件接口

| P3 | T35 球位 | Efinity 端口 | 方向 | 功能 |
|---|---|---|---|---|
| 1 | C13 / GPIOT_RXN00 | `MUX_GPIO1_IN` | 输入 | 相机 Trigger Out 捕获 |
| 2 | E15 / GPIOT_RXN03 | `MUX_GPIO2_OUT` | 输出 | 可配置 Trigger In |
| 3 | E14 / GPIOT_RXN04 | `MUX_GPIO3_IN` | 输入 | UART RX |
| 4 | D14 / GPIOT_RXP04 | `MUX_GPIO4_OUT` | 输出 | UART TX |
| 5 | GND | — | — | 公共参考地 |

所有 P3 信号均为 1.8 V LVCMOS。GPIO1/3 没有内部上下拉。GPIO2 在复位、PLL 未锁定及停止时为低，GPIO4 空闲为高。

## 目录

- `rtl/`：UART、协议、触发发生器、两级同步捕获和顶层。
- `constraints/`：100 MHz 时钟及异步输入到同步器第一级的 CDC 约束。
- `tb/`：Icarus Verilog 自校验 testbench。
- `tools/`：Python 协议库和自动测试 CLI。
- `outflow/`：Efinity compile-only 生成的 bitstream、引脚和时序报告。
- `PROTOCOL.md`：二进制线协议和响应数据结构。
- `BUILD_REPORT.md`：本次仿真、编译、引脚及时序审计结果。

## 验证命令

```bat
cd /d G:\外触发小板子\T35_EXTERNAL_TRIGGER\tb
run_tests.cmd
```

```bat
cd /d G:\外触发小板子\T35_EXTERNAL_TRIGGER
C:\Users\Administrator\AppData\Local\Programs\Python\Python310\python.exe -m unittest discover -s tests -v
```

Efinity 只编译、不下载：

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Users\Administrator\.codex\skills\efinity-auto-build-program\scripts\Invoke-EfinityAutoFlow.ps1 -Project G:\外触发小板子\T35_EXTERNAL_TRIGGER\T35_EXTERNAL_TRIGGER.xml -CompileOnly
```

## PC 工具示例

```bat
cd /d G:\外触发小板子\T35_EXTERNAL_TRIGGER\tools
python t35_trigger_cli.py --port COM5 ping
python t35_trigger_cli.py --port COM5 configure --period-ns 1000000 --width-ns 200
python t35_trigger_cli.py --port COM5 start --count 1000
python t35_trigger_cli.py --port COM5 --json stats.json --csv stats.csv stats
python t35_trigger_cli.py --port COM5 --json loopback.json loopback --period-ns 1000 --width-ns 200 --count 10000
```

参数必须为 10 ns 的正整数倍。若省略 `--port`，系统必须恰好发现一个串口。

## 电气验收边界

- GPIO1 必须使用外部 1.8 V 上拉，且 R104 已拆除；实际阻值仍需记录。
- USB-UART 必须原生支持 1.8 V，或使用合适的双向电平转换。
- 相机 Trigger In 若不接受 1.8 V 推挽，需要外部高速驱动/电平转换。
- 当前 bitstream 未下载到板卡。200 ns 波形、电平、边沿、漏计率和相机兼容性仍必须在 FPGA 引脚附近用示波器验证。

低有效 GPIO2 模式遵循项目基准：停止态固定低，启动后先高电平预充一个完整周期，再输出低脉冲。由于从低有效空闲高回到停止低必然产生额外下降沿，确定性的物理 `LOOPBACK_TEST` 只接受高有效、polarity=`0x00`；低有效波形由 RTL testbench 验证，连接真实相机前必须确认相机对停止转换的处理方式。
