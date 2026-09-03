# 构建与验证报告

日期：2026-09-01  
器件：Efinix Trion T35F324，I4 timing model  
工具：Efinity 2021.2.323.2.18、Icarus Verilog 12.2022.06.11、Python 3.10

## 自动验证结果

- `tb_trigger_generator`：通过；覆盖高/低有效、有限次数、停止低、低有效预充和周期边界原子更新。
- `tb_trigger_capture`：通过；覆盖异步相位 200 ns 低脉冲、10 ns 过窄脉冲、周期/宽度统计和清除。
- `tb_uart`：通过；覆盖分数节拍 UART RX/TX 和空闲高。
- `tb_protocol`：通过；覆盖 PING、未配置错误、配置/极性提交、有限触发、物理回环统计、非法配置不生效、坏 CRC 和重新同步。
- Python `unittest`：4/4 通过；覆盖标准 CRC 向量、帧往返、坏 CRC 和 payload 上限。

## Efinity compile-only

- map：PASS
- Interface Designer：PASS
- place-and-route：PASS
- bitstream generation：PASS
- JTAG programming：SKIPPED
- bitstream：`outflow/T35_EXTERNAL_TRIGGER.bit`
- bitstream 大小：3,047,880 bytes

最终引脚报告确认：C13/GPIO1 输入、E15/GPIO2 输出、E14/GPIO3 输出、D14/GPIO4 输入，均为 Bank 2A、1.8 V LVCMOS，GPIO1/4 Pull Type 为空。

100 MHz 时序收敛：最大路径 setup slack `+0.673 ns`，hold slack `+0.306 ns`，分析最大频率 `107.214 MHz`。未发现 latch、implicit net、unconstrained clock、timing violation 或 Interface Designer issue。

## 告警审计

- `EFX-0034`：包装脚本同时显示命令行源文件和 project XML；Efinity 明确以 project XML 源清单为准，非设计错误。
- `EFX-0200`：CRC16 函数展开后移除冗余中间网络，属于综合优化；CRC 已由 RTL 协议仿真和 Python 标准向量交叉验证。
- `sysclk fabric port unconnected`：Interface Designer 要求 50 MHz PLL 输入同时暴露同名 core port；物理 `sysclk` 已连接 PLL，fabric 副本不使用。尝试删除该 core port会被 Efinity 2021.2 接口检查拒绝，因此保留该良性告警。

没有新增表达式截断、未驱动响应数组、锁存器、隐式网络或未连接 P3 端口告警。
