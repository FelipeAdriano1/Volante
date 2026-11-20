
#NoEnv
#SingleInstance Force

#Include %A_ScriptDir%\Lib\AutoHotInterception.ahk
#Include %A_ScriptDir%\Lib\CvJoyInterface.ahk

#Include %A_ScriptDir%\Modulos\Configuracao.ahk

totalY := 0
pos := 0
referenciaFisica := 0
falhaEixoRegistrada := false
falhaBotoesRegistrada := false
falhaVJoyRegistrada := false
calibrando := false
calibracaoContagem := 0
calibracaoEsquerda := 0
calibracaoDireita := 0
calibracaoEtapa := 0
calibracaoRetorno := 0

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
#Include %A_ScriptDir%\Modulos\Movimento.ahk
#Include %A_ScriptDir%\Modulos\Calibracao.ahk
#Include %A_ScriptDir%\Modulos\Botoes.ahk
#Include %A_ScriptDir%\Modulos\Dispositivos.ahk
#Include %A_ScriptDir%\Modulos\Monitoramento.ahk
#Include %A_ScriptDir%\Modulos\Erros.ahk
#Include %A_ScriptDir%\Modulos\Scroll.ahk

^!End::ExitApp
