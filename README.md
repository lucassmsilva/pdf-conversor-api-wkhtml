# API de Geração de PDF a partir de HTML (.NET 8 + Puppeteer + Docker)

## Descrição

Este projeto implementa uma API REST utilizando .NET 8 Minimal API para converter conteúdo HTML em arquivos PDF. A conversão é realizada utilizando a biblioteca [Puppeteer Sharp](https://github.com/hardkoded/puppeteer-sharp), que controla uma instância headless do Chromium, garantindo alta fidelidade na renderização de HTML, CSS e JavaScript.

A aplicação é projetada para ser executada dentro de um container Docker, que inclui todas as dependências necessárias (runtime .NET, Chromium, bibliotecas de sistema).

## Tecnologias Utilizadas

*   **.NET 8:** Plataforma de desenvolvimento.
*   **ASP.NET Core Minimal API:** Framework para construção da API REST.
*   **Puppeteer Sharp:** Biblioteca .NET para controlar o Chromium/Chrome headless.
*   **Chromium:** Navegador headless utilizado para a renderização do HTML.
*   **Docker:** Plataforma de containerização para empacotamento e distribuição da aplicação e suas dependências.

## Funcionalidades (Endpoints)

A API expõe os seguintes endpoints (por padrão na porta 8080 dentro do container):

1.  **`POST /generate-pdf-from-body`**
    *   **Entrada:** Conteúdo HTML bruto (`text/html`) enviado no corpo da requisição.
    *   **Saída:** Arquivo PDF (`application/pdf`).
    *   **Observação:** CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas (`https://...`) ou estar embutidos diretamente no HTML (tags `<style>`, `<script>`, imagens base64) para serem renderizados corretamente. Links relativos não funcionarão.

2.  **`POST /generate-pdf-from-url`**
    *   **Entrada:** JSON (`application/json`) no corpo da requisição com a estrutura: `{ "url": "SUA_URL_AQUI" }`.
    *   **Saída:** Arquivo PDF (`application/pdf`).
    *   **Observação:** A API navegará até a URL fornecida, carregando todos os seus recursos (CSS, JS, imagens, fontes) como um navegador faria, antes de gerar o PDF. Esta é a abordagem recomendada para melhor fidelidade quando o HTML está hospedado online.

3.  **`POST /generate-pdf-from-file`**
    *   **Entrada:** Upload de arquivo (`multipart/form-data`) com o campo `file` contendo o arquivo HTML.
    *   **Saída:** Arquivo PDF (`application/pdf`) com o nome baseado no arquivo original.
    *   **Observação:** Similar ao endpoint `/generate-pdf-from-body`, CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas ou estar embutidos. Links relativos não funcionarão.

## Como Usar (Docker)

1.  **Construir a Imagem Docker:**
    Navegue até o diretório raiz do projeto (onde está o `Dockerfile`) e execute:
    ```bash
    docker build -t sua-imagem-pdf:1.0 .
    ```
    (Substitua `sua-imagem-pdf:1.0` pelo nome e tag desejados).

2.  **Executar o Container:**
    ```bash
    docker run -d -p 8081:8080 --name container-pdf sua-imagem-pdf:1.0
    ```
    *   `-d`: Executa em modo detached (background).
    *   `-p 8081:8080`: Mapeia a porta `8081` da sua máquina host para a porta `8080` dentro do container (onde a API escuta por padrão). Você pode alterar a porta do host (`8081`) se necessário.
    *   `--name container-pdf`: Dá um nome ao container para facilitar o gerenciamento.
    *   `sua-imagem-pdf:1.0`: O nome da imagem que você construiu.

3.  **Acessar a API:**
    *   **Swagger UI:** Abra seu navegador e acesse `http://localhost:8081/`. Você será redirecionado para a interface do Swagger para explorar e testar os endpoints.
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

## Prós e Contras da Abordagem

### Prós

*   **Alta Fidelidade de Renderização:** O uso do Puppeteer/Chromium garante que o PDF final seja uma representação muito precisa da página HTML como vista em um navegador moderno, incluindo CSS complexo, fontes customizadas (se instaladas no container) e execução de JavaScript.
*   **Suporte a Padrões Web Modernos:** Capaz de processar HTML5, CSS3 e JavaScript ES6+.
*   **Flexibilidade de Entrada:** A API oferece múltiplas formas de fornecer o HTML (corpo, URL, arquivo). O método por URL é particularmente poderoso, pois lida automaticamente com recursos externos.
*   **Containerização (Docker):**
    *   **Portabilidade:** A aplicação e *todas* as suas dependências (runtime, Chromium, libs) estão empacotadas, garantindo que funcione de forma consistente em diferentes ambientes (desenvolvimento, teste, produção).
    *   **Isolamento:** O container isola a aplicação do sistema operacional hospedeiro.
    *   **Facilidade de Implantação:** Simplifica o processo de deploy em servidores ou serviços de nuvem que suportam Docker.
*   **Plataforma .NET Moderna:** Benefícios do .NET 8 e Minimal APIs, como performance, menor boilerplate e recursos de linguagem modernos.

### Contras

*   **Tamanho da Imagem Docker:** A inclusão do Chromium e suas dependências aumenta significativamente o tamanho final da imagem Docker (pode facilmente passar de 1GB), comparado a soluções sem browser.
*   **Consumo de Recursos (CPU/Memória):** Cada requisição para gerar PDF lança (ou reutiliza de um pool, se otimizado) uma instância do navegador headless. Isso consome mais CPU e memória RAM do que bibliotecas de conversão direta (como iTextSharp, QuestPDF, ou mesmo wkhtmltopdf), especialmente sob alta carga concorrente. Requer provisionamento adequado de recursos no servidor.
*   **Dependências do Sistema Operacional:** O Chromium tem dependências de bibliotecas gráficas e de sistema do Linux (instaladas via `apt-get` no Dockerfile). Isso torna a execução *fora* de um container Docker (ou de um ambiente Linux corretamente configurado) mais complexa.
*   **Tempo de Inicialização do Browser:** Pode haver uma pequena latência na primeira requisição ou em cenários de "cold start" (como em serverless) devido ao tempo necessário para iniciar o processo do Chromium.
*   **Complexidade no Tratamento de Links Relativos:** Para os endpoints que recebem HTML bruto (`/generate-pdf-from-body`, `/generate-pdf-from-file`), lidar corretamente com links relativos para CSS/imagens é problemático e geralmente exige que esses recursos usem URLs absolutas ou estejam embutidos.
*   **Segurança (`--no-sandbox`):** A flag `--no-sandbox`, frequentemente necessária para rodar o Chromium como root dentro de containers Docker, desabilita uma camada de segurança importante do navegador. É crucial garantir que o container esteja adequadamente isolado na rede e não processe HTML de fontes não confiáveis sem sanitização, se a segurança for crítica.

## Considerações Adicionais

*   **Fontes:** Se o seu HTML utiliza fontes específicas, elas precisam ser instaladas dentro do container Docker (via `apt-get install fonts-...` ou copiando arquivos `.ttf`/`.otf` e rodando `fc-cache -fv`). A fonte `fonts-liberation` é incluída no Dockerfile de exemplo como uma base.
*   **Alternativas:** Para casos mais simples ou com requisitos de performance/tamanho mais estritos, bibliotecas como [QuestPDF](https://www.questpdf.com/) (geração programática), [IronPDF](https://ironpdf.com/) (comercial, similar ao Puppeteer mas integrada) ou wrappers para `wkhtmltopdf` (menos fiel com JS moderno) podem ser consideradas.
*   **Otimizações:** Para alta carga, pode-se investigar manter uma instância do browser Puppeteer aberta entre requisições (pooling) em vez de lançar uma nova a cada vez, mas isso adiciona complexidade no gerenciamento do ciclo de vida do browser.