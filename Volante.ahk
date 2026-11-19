
#NoEnv
#SingleInstance Force
#Include %A_ScriptDir%\Lib\AutoHotInterception.ahk
#Include %A_ScriptDir%\Lib\CvJoyInterface.ahk


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


IniRead, DISPOSITIVO, %ConfigPath%, Sistema, DispositivoVJoy, 1

totalY := 0
pos := 0
falhaEixoRegistrada := false
falhaBotoesRegistrada := false
falhaVJoyRegistrada := false
calibrando := false
calibracaoContagem := 0
calibracaoEsquerda := 0
calibracaoDireita := 0

; =================================

; ===== INICIALIZACAO DO VJOY =====

vjoy := new CvJoyInterface()

; Verifica se a biblioteca e o driver estao funcionando
if (!vjoy.vJoyEnabled()) {
    MsgBox, 16, Erro no vJoy, % "Nao foi possivel inicializar o vJoy.`n`nDetalhes:`n" vjoy.LoadLibraryLog
    ExitApp
}

; Verifica se o dispositivo solicitado existe
volante := vjoy.Devices[DISPOSITIVO]

if (!volante.IsEnabled()) {
    MsgBox, 16, Erro no vJoy, % "O dispositivo vJoy " DISPOSITIVO " nao esta habilitado ou nao existe."
    ExitApp
}

; Verifica se o dispositivo pode ser utilizado
if (!volante.IsAvailable()) {
    MsgBox, 16, Erro no vJoy, % "O dispositivo vJoy " DISPOSITIVO " esta indisponivel ou ocupado."
    ExitApp
}

; Inicializa eixo e verifica o resultado
if (!volante.SetAxisByIndex(CENTRO, EIXO)) {
    MsgBox, 16, Erro no vJoy, Falha ao inicializar o eixo do volante.
    ExitApp
}

; Inicializa AutoHotInterception
try {
    AHI := new AutoHotInterception()
}
catch e {
    RegistrarErro("Falha ao inicializar AHI: " e.Message)

    MsgBox, 16, Erro no AHI, % "Nao foi possivel inicializar o AutoHotInterception.`n`nDetalhes:`n" e.Message
    ExitApp
}
mouseConectado := false
falhaConsultaRegistrada := false

ToolTip, Aguardando mouse do volante..., 10, 100, 2

; Verifica imediatamente se o mouse esta conectado
Gosub, VerificarMouse

SetTimer, Mostrar, 100
SetTimer, VerificarMouse, 1000
SetTimer, VerificarVJoy, 1000
return

; ===== DETECCAO E RECONECTACAO =====

VerificarMouse:
global AHI, VID, PID, mouseConectado, id
global falhaConsultaRegistrada

; Consulta dispositivos
try {
    lista := AHI.GetDeviceList()
    falhaConsultaRegistrada := false
}
catch e {
    if (!falhaConsultaRegistrada) {
        RegistrarErro("Falha ao consultar dispositivos AHI: " e.Message)
        falhaConsultaRegistrada := true
    }

    ToolTip, Erro na comunicacao com AHI. Consulte log.txt, 10, 100, 2
    return
}

encontrado := false
novoId := 0

for indice, dispositivo in lista {
    if (dispositivo.isMouse
        && dispositivo.Vid = VID
        && dispositivo.Pid = PID) {

        encontrado := true
        novoId := dispositivo.ID
        break
    }
}

; Mouse desconectado
if (!encontrado && mouseConectado) {
    mouseConectado := false
    ToolTip, Mouse do volante desconectado!, 10, 100, 2
}

; Mouse conectado ou reconectado
if (encontrado && !mouseConectado) {

    if (AssinarMouse(novoId)) {
        id := novoId
        mouseConectado := true

        ToolTip, Mouse conectado! ID: %id%, 10, 100, 2
    }
}
return

; ===== ASSINATURA DO MOUSE =====

AssinarMouse(idMouse) {
    global AHI

    try {
        AHI.SubscribeMouseMove(idMouse, true, Func("Mexeu"))
        AHI.SubscribeMouseButtons(idMouse, true, Func("BotaoMouse"))

        return true
    }
    catch e {

        ; Tenta desfazer uma assinatura parcial
        try {
            AHI.UnsubscribeMouseMove(idMouse)
        }
        catch e2 {
        }

        try {
            AHI.UnsubscribeMouseButtons(idMouse)
        }
        catch e2 {
        }

        RegistrarErro("Falha ao assinar mouse ID " idMouse ": " e.Message)

        ToolTip, Falha ao ativar mouse. Consulte log.txt, 10, 100, 2

        return false
    }
}

; ===== MOVIMENTO DO VOLANTE =====

Mexeu(x, y) {
    global totalY, pos, LIMITE, SINAL, volante, EIXO, CENTRO
    global falhaEixoRegistrada
	global calibrando, calibracaoContagem
	
    totalY += y
	if (calibrando) {
        calibracaoContagem += y
        return
    }

    pos += y * SINAL
    pos := Max(-LIMITE, Min(LIMITE, pos))

    eixo := Round(CENTRO + (CENTRO * pos / LIMITE))

    try {
        resultado := volante.SetAxisByIndex(eixo, EIXO)

        if (!resultado) {
            if (!falhaEixoRegistrada) {
                RegistrarErro("Falha ao enviar movimento para o vJoy.")
                falhaEixoRegistrada := true
            }
        }
        else {
            falhaEixoRegistrada := false
        }
    }
    catch e {
        if (!falhaEixoRegistrada) {
            RegistrarErro("Erro no movimento: " e.Message)
            falhaEixoRegistrada := true
        }
    }
}

; ===== BOTOES E SCROLL =====

BotaoMouse(codigo, estado) {
    global volante
    global BTN_ESQUERDO, BTN_DIREITO, BTN_CENTRAL
    global BTN_SCROLL_CIMA, BTN_SCROLL_BAIXO
    global DURACAO_SCROLL
    global falhaBotoesRegistrada

    botao := 0

    if (codigo = 0)
        botao := BTN_ESQUERDO

    else if (codigo = 1)
        botao := BTN_DIREITO

    else if (codigo = 2)
        botao := BTN_CENTRAL

    else if (codigo = 5) {
        if (estado = 1) {
            botao := BTN_SCROLL_CIMA
            SetTimer, SoltarScrollCima, % -DURACAO_SCROLL
        }
        else if (estado = -1) {
            botao := BTN_SCROLL_BAIXO
            SetTimer, SoltarScrollBaixo, % -DURACAO_SCROLL
        }
    }

    if (!botao)
        return

    try {
        resultado := volante.SetBtn(estado = -1 ? 1 : estado, botao)

        if (!resultado) {
            if (!falhaBotoesRegistrada) {
                RegistrarErro("Falha ao enviar comando de botao para o vJoy.")
                falhaBotoesRegistrada := true
            }
        }
        else {
            falhaBotoesRegistrada := false
        }
    }
    catch e {
        if (!falhaBotoesRegistrada) {
            RegistrarErro("Erro nos botoes: " e.Message)
            falhaBotoesRegistrada := true
        }
    }
}

; ===== MONITORAMENTO =====
Mostrar:
ToolTip, Posicao: %pos%`nContagens totais: %totalY%, 10, 50, 1
return

; ===== CENTRALIZAR =====
F8::
pos := 0

if (!volante.SetAxisByIndex(CENTRO, EIXO)) {
    RegistrarErro("Falha ao centralizar o volante.")
}
return

; ===== REGISTRO DE ERROS =====

RegistrarErro(mensagem) {
    FormatTime, dataHora,, yyyy-MM-dd HH:mm:ss

    linha := dataHora " | " mensagem "`r`n"

    FileAppend, %linha%, %A_ScriptDir%\log.txt
}

F9::
if (calibrando) {
    MsgBox, 48, Calibracao, A calibracao ja esta em andamento.
    return
}

calibrando := true
calibracaoContagem := 0
calibracaoEsquerda := 0
calibracaoDireita := 0

MsgBox, 64, Calibracao, Calibracao iniciada!`n`nCentralize fisicamente o volante antes de continuar.`n`nGire 2,5 voltas para a esquerda e pressione F10.
return

F10::
if (!calibrando) {
    MsgBox, 48, Calibracao, Inicie a calibracao primeiro com F9.
    return
}

calibracaoEsquerda := Abs(calibracaoContagem)
calibracaoContagem := 0

MsgBox, 64, Calibracao, Limite esquerdo registrado: %calibracaoEsquerda%`n`nAgora retorne o volante ao centro e pressione F11.
return

F11::
if (!calibrando) {
    MsgBox, 48, Calibracao, Inicie a calibracao primeiro com F9.
    return
}

calibracaoContagem := 0

MsgBox, 64, Calibracao, Centro registrado!`n`nAgora gire 2,5 voltas para a direita e pressione F12.
return

F12::
if (!calibrando) {
    MsgBox, 48, Calibracao, Inicie a calibracao primeiro com F9.
    return
}

calibracaoDireita := Abs(calibracaoContagem)
calibrando := false

MsgBox, 64, Calibracao concluida, Calibracao finalizada!`n`nLimite esquerdo: %calibracaoEsquerda%`nLimite direito: %calibracaoDireita%`n`nEsses valores ainda nao foram salvos no Config.ini.
return

Esc::ExitApp

SoltarScrollCima:
global volante, BTN_SCROLL_CIMA
volante.SetBtn(0, BTN_SCROLL_CIMA)
return

SoltarScrollBaixo:
global volante, BTN_SCROLL_BAIXO
volante.SetBtn(0, BTN_SCROLL_BAIXO)
return

; ===== VERIFICACAO DO VJOY =====

VerificarVJoy:
status := volante.GetStatus()

if (status != vjoy.VJD_STAT_OWN) {

    if (!falhaVJoyRegistrada) {
        RegistrarErro("Dispositivo vJoy perdeu o estado OWN. Estado atual: " volante.GetStatusName())

        falhaVJoyRegistrada := true
    }

    ToolTip, Erro no vJoy! Consulte log.txt, 10, 100, 2
}
else {
    if (falhaVJoyRegistrada) {
        ToolTip, vJoy voltou a funcionar!, 10, 100, 2
    }

    falhaVJoyRegistrada := false
}
return
