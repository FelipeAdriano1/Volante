
; ===== DETECCAO DE DISPOSITIVOS =====

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


; ===== ASSINATURA DOS EVENTOS =====

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
