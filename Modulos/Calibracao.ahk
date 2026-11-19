
; ===== CALIBRAGEM DO VOLANTE =====

F9::
if (calibrando) {
    MsgBox, 48, Calibracao, A calibracao ja esta em andamento.
    return
}

calibrando := true
calibracaoEtapa := 1
calibracaoContagem := 0
calibracaoEsquerda := 0
calibracaoDireita := 0
calibracaoRetorno := 0

MsgBox, 64, Calibracao, Calibracao iniciada!`n`nCentralize fisicamente o volante.`n`nGire 2,5 voltas para a esquerda e pressione F10.
return


F10::
if (!calibrando || calibracaoEtapa != 1) {
    MsgBox, 48, Calibracao, Etapa incorreta. Inicie ou siga a calibracao na ordem indicada.
    return
}

calibracaoEsquerda := Abs(calibracaoContagem)

if (calibracaoEsquerda <= 0) {
    MsgBox, 48, Calibracao, Nenhum movimento foi registrado para a esquerda. Tente novamente.
    return
}

calibracaoEtapa := 2
calibracaoContagem := 0

MsgBox, 64, Calibracao, Limite esquerdo registrado: %calibracaoEsquerda%`n`nRetorne o volante ao centro e pressione F11.
return


F11::
if (!calibrando || calibracaoEtapa != 2) {
    MsgBox, 48, Calibracao, Registre primeiro o limite esquerdo com F10.
    return
}

calibracaoRetorno := Abs(calibracaoContagem)

if (calibracaoRetorno <= 0) {
    MsgBox, 48, Calibracao, Nenhum movimento de retorno foi registrado. Volte o volante ao centro e tente novamente.
    return
}

calibracaoEtapa := 3
calibracaoContagem := 0

MsgBox, 64, Calibracao, Retorno registrado!`n`nAgora gire 2,5 voltas para a direita e pressione F12.
return

F12::
if (!calibrando || calibracaoEtapa != 3) {
    MsgBox, 48, Calibracao, Complete as etapas anteriores antes de registrar a direita.
    return
}

calibracaoDireita := Abs(calibracaoContagem)

if (calibracaoDireita <= 0) {
    MsgBox, 48, Calibracao, Nenhum movimento foi registrado para a direita. Tente novamente.
    return
}

; ===== APLICAR LIMITES CALIBRADOS =====

LIMITE_ESQUERDO := calibracaoEsquerda
LIMITE_DIREITO := calibracaoDireita

; Define a referencia fisica no ponto extremo direito
referenciaFisica := totalY - calibracaoContagem

; Calcula a posicao atual
pos := (totalY - referenciaFisica) * SINAL

if (pos < 0)
    eixo := Round(CENTRO + (CENTRO * pos / LIMITE_ESQUERDO))
else
    eixo := Round(CENTRO + (CENTRO * pos / LIMITE_DIREITO))

; Atualiza o eixo virtual
if (!volante.SetAxisByIndex(eixo, EIXO))
    RegistrarErro("Falha ao atualizar eixo apos a calibracao.")

; ===== SALVAR NO CONFIG.INI =====

IniWrite, %LIMITE_ESQUERDO%, %ConfigPath%, Volante, LimiteEsquerdo
erroEsquerdo := ErrorLevel

IniWrite, %LIMITE_DIREITO%, %ConfigPath%, Volante, LimiteDireito
erroDireito := ErrorLevel

calibrando := false
calibracaoEtapa := 0

if (erroEsquerdo || erroDireito) {
    MsgBox, 48, Calibracao, Calibracao aplicada na memoria, mas houve uma falha ao salvar os valores no Config.ini.
}
else {
    MsgBox, 64, Calibracao concluida, Calibracao finalizada com sucesso!`n`nLimite esquerdo: %LIMITE_ESQUERDO%`nLimite direito: %LIMITE_DIREITO%`nRetorno: %calibracaoRetorno%`n`nValores salvos no Config.ini.
}
return
