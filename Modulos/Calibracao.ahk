
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

calibrando := false
calibracaoEtapa := 0

MsgBox, 64, Calibracao concluida, Calibracao finalizada!`n`nLimite esquerdo: %calibracaoEsquerda%`nLimite direito: %calibracaoDireita%`nRetorno: %calibracaoRetorno%`n`nEsses valores ainda nao foram salvos no Config.ini.
return
