; ===== CARREGAR CONFIGURACOES =====

ConfigPath := A_ScriptDir "\Config.ini"

IniRead, VID, %ConfigPath%, Mouse, VID, 0x30FA
IniRead, PID, %ConfigPath%, Mouse, PID, 0x0302
VID := VID + 0
PID := PID + 0

IniRead, LIMITE, %ConfigPath%, Volante, Limite, 9900
IniRead, SINAL, %ConfigPath%, Volante, Sinal, 1
IniRead, EIXO, %ConfigPath%, Volante, Eixo, 2
IniRead, CENTRO, %ConfigPath%, Volante, Centro, 16384

; Configuracao dos botoes
IniRead, BTN_ESQUERDO, %ConfigPath%, Botoes, CliqueEsquerdo, 1
IniRead, BTN_DIREITO, %ConfigPath%, Botoes, CliqueDireito, 2
IniRead, BTN_CENTRAL, %ConfigPath%, Botoes, CliqueCentral, 3
IniRead, BTN_SCROLL_CIMA, %ConfigPath%, Botoes, ScrollCima, 4
IniRead, BTN_SCROLL_BAIXO, %ConfigPath%, Botoes, ScrollBaixo, 5

; Configuracao do scroll
IniRead, DURACAO_SCROLL, %ConfigPath%, Scroll, Duracao, 120

; Configuracao do sistema
IniRead, DISPOSITIVO, %ConfigPath%, Sistema, DispositivoVJoy, 1
