
; ===== CONTROLE DO SCROLL =====

SoltarScrollCima:
global volante, BTN_SCROLL_CIMA

try {
    if (!volante.SetBtn(0, BTN_SCROLL_CIMA))
        RegistrarErro("Falha ao soltar o scroll para cima.")
}
catch e {
    RegistrarErro("Erro ao soltar o scroll para cima: " e.Message)
}
return


SoltarScrollBaixo:
global volante, BTN_SCROLL_BAIXO

try {
    if (!volante.SetBtn(0, BTN_SCROLL_BAIXO))
        RegistrarErro("Falha ao soltar o scroll para baixo.")
}
catch e {
    RegistrarErro("Erro ao soltar o scroll para baixo: " e.Message)
}
return
