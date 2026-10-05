# VS01 原创合成音频记录

2026-10-05。来源为项目内 tools/audio/generate-vs01-audio.mjs，自行算法合成；种子 20261005，44100Hz、16-bit、mono PCM。逐项时长、SHA256及来源见 assets/audio/vs01/manifest.json。

拨弦主题使用 Karplus-Strong 延迟线和自行排列的基础和弦/音符；它是合成拨弦，不是吉他录音。脚步、碗筷、硬币、糖纸及环境为噪声包络/合成共振，不声称真实实地录音。没有外部音乐、商业歌曲、采样包、演员声音或声音克隆，没有网络运行依赖。

这是本项目创作来源记录，不是第三方 CC0 授权证书；发行前仍由项目作者核对最终素材和发行权利。不得将听感测试和版权核查混为一谈。

生成：node tools/audio/generate-vs01-audio.mjs --output assets/audio/vs01
验证：node tools/audio/check-vs01-audio.mjs（独立重复生成、WAV参数、哈希、峰值）。

截至本次实现：参数及引擎加载已安排自动验证；真实音箱/耳机试听、循环听感和混音平衡待用户确认，未标注为已通过。
