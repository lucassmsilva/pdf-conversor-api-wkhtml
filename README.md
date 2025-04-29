# API de Gera��o de PDF a partir de HTML (.NET 8 + Puppeteer + Docker)

## Descri��o

Este projeto implementa uma API REST utilizando .NET 8 Minimal API para converter conte�do HTML em arquivos PDF. A convers�o � realizada utilizando a biblioteca [Puppeteer Sharp](https://github.com/hardkoded/puppeteer-sharp), que controla uma inst�ncia headless do Chromium, garantindo alta fidelidade na renderiza��o de HTML, CSS e JavaScript.

A aplica��o � projetada para ser executada dentro de um container Docker, que inclui todas as depend�ncias necess�rias (runtime .NET, Chromium, bibliotecas de sistema).

## Tecnologias Utilizadas

*   **.NET 8:** Plataforma de desenvolvimento.
*   **ASP.NET Core Minimal API:** Framework para constru��o da API REST.
*   **Puppeteer Sharp:** Biblioteca .NET para controlar o Chromium/Chrome headless.
*   **Chromium:** Navegador headless utilizado para a renderiza��o do HTML.
*   **Docker:** Plataforma de containeriza��o para empacotamento e distribui��o da aplica��o e suas depend�ncias.

## Funcionalidades (Endpoints)

A API exp�e os seguintes endpoints (por padr�o na porta 8080 dentro do container):

1.  **`POST /generate-pdf-from-body`**
    *   **Entrada:** Conte�do HTML bruto (`text/html`) enviado no corpo da requisi��o.
    *   **Sa�da:** Arquivo PDF (`application/pdf`).
    *   **Observa��o:** CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas (`https://...`) ou estar embutidos diretamente no HTML (tags `<style>`, `<script>`, imagens base64) para serem renderizados corretamente. Links relativos n�o funcionar�o.

2.  **`POST /generate-pdf-from-url`**
    *   **Entrada:** JSON (`application/json`) no corpo da requisi��o com a estrutura: `{ "url": "SUA_URL_AQUI" }`.
    *   **Sa�da:** Arquivo PDF (`application/pdf`).
    *   **Observa��o:** A API navegar� at� a URL fornecida, carregando todos os seus recursos (CSS, JS, imagens, fontes) como um navegador faria, antes de gerar o PDF. Esta � a abordagem recomendada para melhor fidelidade quando o HTML est� hospedado online.

3.  **`POST /generate-pdf-from-file`**
    *   **Entrada:** Upload de arquivo (`multipart/form-data`) com o campo `file` contendo o arquivo HTML.
    *   **Sa�da:** Arquivo PDF (`application/pdf`) com o nome baseado no arquivo original.
    *   **Observa��o:** Similar ao endpoint `/generate-pdf-from-body`, CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas ou estar embutidos. Links relativos n�o funcionar�o.

## Como Usar (Docker)

1.  **Construir a Imagem Docker:**
    Navegue at� o diret�rio raiz do projeto (onde est� o `Dockerfile`) e execute:
    ```bash
    docker build -t sua-imagem-pdf:1.0 .
    ```
    (Substitua `sua-imagem-pdf:1.0` pelo nome e tag desejados).

2.  **Executar o Container:**
    ```bash
    docker run -d -p 8081:8080 --name container-pdf sua-imagem-pdf:1.0
    ```
    *   `-d`: Executa em modo detached (background).
    *   `-p 8081:8080`: Mapeia a porta `8081` da sua m�quina host para a porta `8080` dentro do container (onde a API escuta por padr�o). Voc� pode alterar a porta do host (`8081`) se necess�rio.
    *   `--name container-pdf`: D� um nome ao container para facilitar o gerenciamento.
    *   `sua-imagem-pdf:1.0`: O nome da imagem que voc� construiu.

3.  **Acessar a API:**
    *   **Swagger UI:** Abra seu navegador e acesse `http://localhost:8081/`. Voc� ser� redirecionado para a interface do Swagger para explorar e testar os endpoints.
    *   **Endpoints (Exemplos com `curl`):**

        *   **Via Corpo HTML (usando um arquivo `teste.html`):**
            ```bash
            curl -X POST "http://localhost:8081/generate-pdf-from-body" \
                 -H "Content-Type: text/html" \
                 --data-binary "@teste.html" \
                 -o output_body.pdf --fail
            ```

        *   **Via URL:**
            ```bash
            curl -X POST "http://localhost:8081/generate-pdf-from-url" \
                 -H "Content-Type: application/json" \
                 -d "{\"url\": \"https://example.com\"}" \
                 -o output_url.pdf --fail
            ```

        *   **Via Upload de Arquivo (usando um arquivo `teste.html`):**
            ```bash
            curl -X POST "http://localhost:8081/generate-pdf-from-file" \
                 -F "file=@teste.html" \
                 -o output_file.pdf --fail
            ```

## Pr�s e Contras da Abordagem

### Pr�s

*   **Alta Fidelidade de Renderiza��o:** O uso do Puppeteer/Chromium garante que o PDF final seja uma representa��o muito precisa da p�gina HTML como vista em um navegador moderno, incluindo CSS complexo, fontes customizadas (se instaladas no container) e execu��o de JavaScript.
*   **Suporte a Padr�es Web Modernos:** Capaz de processar HTML5, CSS3 e JavaScript ES6+.
*   **Flexibilidade de Entrada:** A API oferece m�ltiplas formas de fornecer o HTML (corpo, URL, arquivo). O m�todo por URL � particularmente poderoso, pois lida automaticamente com recursos externos.
*   **Containeriza��o (Docker):**
    *   **Portabilidade:** A aplica��o e *todas* as suas depend�ncias (runtime, Chromium, libs) est�o empacotadas, garantindo que funcione de forma consistente em diferentes ambientes (desenvolvimento, teste, produ��o).
    *   **Isolamento:** O container isola a aplica��o do sistema operacional hospedeiro.
    *   **Facilidade de Implanta��o:** Simplifica o processo de deploy em servidores ou servi�os de nuvem que suportam Docker.
*   **Plataforma .NET Moderna:** Benef�cios do .NET 8 e Minimal APIs, como performance, menor boilerplate e recursos de linguagem modernos.

### Contras

*   **Tamanho da Imagem Docker:** A inclus�o do Chromium e suas depend�ncias aumenta significativamente o tamanho final da imagem Docker (pode facilmente passar de 1GB), comparado a solu��es sem browser.
*   **Consumo de Recursos (CPU/Mem�ria):** Cada requisi��o para gerar PDF lan�a (ou reutiliza de um pool, se otimizado) uma inst�ncia do navegador headless. Isso consome mais CPU e mem�ria RAM do que bibliotecas de convers�o direta (como iTextSharp, QuestPDF, ou mesmo wkhtmltopdf), especialmente sob alta carga concorrente. Requer provisionamento adequado de recursos no servidor.
*   **Depend�ncias do Sistema Operacional:** O Chromium tem depend�ncias de bibliotecas gr�ficas e de sistema do Linux (instaladas via `apt-get` no Dockerfile). Isso torna a execu��o *fora* de um container Docker (ou de um ambiente Linux corretamente configurado) mais complexa.
*   **Tempo de Inicializa��o do Browser:** Pode haver uma pequena lat�ncia na primeira requisi��o ou em cen�rios de "cold start" (como em serverless) devido ao tempo necess�rio para iniciar o processo do Chromium.
*   **Complexidade no Tratamento de Links Relativos:** Para os endpoints que recebem HTML bruto (`/generate-pdf-from-body`, `/generate-pdf-from-file`), lidar corretamente com links relativos para CSS/imagens � problem�tico e geralmente exige que esses recursos usem URLs absolutas ou estejam embutidos.
*   **Seguran�a (`--no-sandbox`):** A flag `--no-sandbox`, frequentemente necess�ria para rodar o Chromium como root dentro de containers Docker, desabilita uma camada de seguran�a importante do navegador. � crucial garantir que o container esteja adequadamente isolado na rede e n�o processe HTML de fontes n�o confi�veis sem sanitiza��o, se a seguran�a for cr�tica.

## Considera��es Adicionais

*   **Fontes:** Se o seu HTML utiliza fontes espec�ficas, elas precisam ser instaladas dentro do container Docker (via `apt-get install fonts-...` ou copiando arquivos `.ttf`/`.otf` e rodando `fc-cache -fv`). A fonte `fonts-liberation` � inclu�da no Dockerfile de exemplo como uma base.
*   **Alternativas:** Para casos mais simples ou com requisitos de performance/tamanho mais estritos, bibliotecas como [QuestPDF](https://www.questpdf.com/) (gera��o program�tica), [IronPDF](https://ironpdf.com/) (comercial, similar ao Puppeteer mas integrada) ou wrappers para `wkhtmltopdf` (menos fiel com JS moderno) podem ser consideradas.
*   **Otimiza��es:** Para alta carga, pode-se investigar manter uma inst�ncia do browser Puppeteer aberta entre requisi��es (pooling) em vez de lan�ar uma nova a cada vez, mas isso adiciona complexidade no gerenciamento do ciclo de vida do browser.