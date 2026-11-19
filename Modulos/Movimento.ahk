
; ===== MOVIMENTO DO VOLANTE =====

Mexeu(x, y) {
    global totalY, pos, referenciaFisica, SINAL
    global LIMITE_ESQUERDO, LIMITE_DIREITO
    global volante, EIXO, CENTRO
    global falhaEixoRegistrada
    global calibrando, calibracaoContagem

    totalY += y

    if (calibrando) {
        calibracaoContagem += y
        return
    }

    pos := (totalY - referenciaFisica) * SINAL

    if (pos < 0) {
        pos := Max(-LIMITE_ESQUERDO, pos)
        eixo := Round(CENTRO + (CENTRO * pos / LIMITE_ESQUERDO))
    }
    else {
        pos := Min(LIMITE_DIREITO, pos)
        eixo := Round(CENTRO + (CENTRO * pos / LIMITE_DIREITO))
    }

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
