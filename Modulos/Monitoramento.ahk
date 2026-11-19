
; ===== MONITORAMENTO =====
Mostrar:
mensagem := "Posicao: " pos "`nContagens totais: " totalY

if (calibrando)
    mensagem .= "`n`nCALIBRACAO EM ANDAMENTO!"

ToolTip, %mensagem%, 10, 50, 1
return

; ===== VERIFICACAO DO VJOY =====

VerificarVJoy:
try {
    status := volante.GetStatus()
}
catch e {
    if (!falhaVJoyRegistrada) {
        RegistrarErro("Erro ao consultar estado do vJoy: " e.Message)
        falhaVJoyRegistrada := true
    }

    ToolTip, Erro na comunicacao com vJoy! Consulte log.txt, 10, 100, 2
    return
}

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
