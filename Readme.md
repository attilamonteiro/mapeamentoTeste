# MapeamentoTeste

Este projeto é uma API ASP.NET Core que utiliza Entity Framework Core para gerenciar um banco de dados SQLite. A API permite operações CRUD (Create, Read, Update, Delete) em entidades de Produto e Categoria.

## Configuração do Projeto

### Pré-requisitos

- .NET 6 SDK ou superior
- SQLite

### Instalação

1.  Clone o repositório:

    ```bash
    git clone https://github.com/SeuUsuario/mapeamentoTeste.git
    cd mapeamentoTeste/mapeamentoTeste
    ```

2.  Restaure as dependências do projeto:

    ```bash
    dotnet restore
    ```

3.  Configure a string de conexão no arquivo `appsettings.json`:

    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Data Source=mapeamentoTeste.db"
      }
    }
    ```

4.  Execute as migrações para criar o banco de dados:

    ```bash
    Add-Migration InitialMigration
    ```

        ```bash

    Update-Database

    ```

    ```

### Executando o Projeto

Para iniciar a aplicação, execute o comando:

```bash
dotnet run
```

A API estará disponível em `https://localhost:5001` ou `http://localhost:5000`.

## Endpoints da API

### Produtos

- `GET /api/produtos`: Retorna todos os produtos.
- `GET /api/produtos/{id}`: Retorna um produto pelo ID.
- `POST /api/produtos`: Cria um novo produto.
- `PUT /api/produtos/{id}`: Atualiza um produto existente.
- `DELETE /api/produtos/{id}`: Exclui um produto pelo ID.

### Categorias

- `GET /api/categorias`: Retorna todas as categorias.
- `GET /api/categorias/{id}`: Retorna uma categoria pelo ID.
- `POST /api/categorias`: Cria uma nova categoria.
- `PUT /api/categorias/{id}`: Atualiza uma categoria existente.
- `DELETE /api/categorias/{id}`: Exclui uma categoria pelo ID.

## Exemplos de Requisições

### Criar um Produto

```bash
curl -X POST "https://localhost:5001/api/produtos" -H "Content-Type: application/json" -d '{
  "nome": "Produto Exemplo",
  "preco": 10.99,
  "categoriaId": 1
}'
```

### Atualizar uma Categoria

```bash
curl -X PUT "https://localhost:5001/api/categorias/1" -H "Content-Type: application/json" -d '{
  "nome": "Categoria Atualizada",
  "descricao": "Descrição atualizada da categoria"
}'
```

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests.

## Licença

Este projeto está licenciado sob a Licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
