
; ===== MOVIMENTO DO VOLANTE =====

Mexeu(x, y) {
    global totalY, pos, referenciaFisica, LIMITE, SINAL, volante, EIXO, CENTRO
    global falhaEixoRegistrada
    global calibrando, calibracaoContagem

    totalY += y

    if (calibrando) {
        calibracaoContagem += y
        return
    }

    pos := (totalY - referenciaFisica) * SINAL
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

; ===== CENTRALIZAR =====

F8::
referenciaFisica := totalY
pos := 0

if (!volante.SetAxisByIndex(CENTRO, EIXO)) {
    RegistrarErro("Falha ao centralizar o volante.")
}
return
