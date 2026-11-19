
; ===== REGISTRO DE ERROS =====

RegistrarErro(mensagem) {
    FormatTime, dataHora,, yyyy-MM-dd HH:mm:ss

    linha := dataHora " | " mensagem "`r`n"

    try {
        FileAppend, %linha%, %A_ScriptDir%\log.txt
    }
    catch e {
        MsgBox, 16, Erro no registro, Nao foi possivel gravar o arquivo de log.txt.`n`nDetalhes: %e%
    }
}
