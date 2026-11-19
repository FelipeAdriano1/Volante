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