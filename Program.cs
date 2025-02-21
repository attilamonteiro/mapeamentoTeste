using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Configuração do banco de dados SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Habilita CORS para permitir chamadas da API de outros domínios
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware para ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll"); // Aplica política CORS
app.UseAuthorization();
app.MapControllers();

app.Run();


// ======================= CONTEXTO DO BANCO DE DADOS ============================
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos { get; set; }
    public DbSet<Categoria> Categorias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Produto).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Categoria).Assembly);
    }
}


// ======================= CONFIGURAÇÃO DAS ENTIDADES ============================
public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome)
               .IsRequired()
               .HasMaxLength(100);
        builder.Property(p => p.Preco)
               .HasColumnType("REAL"); // SQLite não suporta decimal(18,2)
        builder.HasOne(p => p.Categoria)
               .WithMany(c => c.Produtos)
               .HasForeignKey(p => p.CategoriaId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome)
               .IsRequired()
               .HasMaxLength(50);
        builder.Property(c => c.Descricao)
               .HasMaxLength(200);
    }
}


// ======================= ENTIDADES ============================
public class Produto
{
    [BindNever]
    public int Id { get; set; }  // Gerado automaticamente pelo banco de dados

    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    public string Nome { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Preco { get; set; }

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public int CategoriaId { get; set; }

    public Categoria? Categoria { get; set; }
}

public class Categoria
{
    [BindNever]
    public int Id { get; set; }  // Gerado automaticamente pelo banco de dados

    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    public string Nome { get; set; }

    public string Descricao { get; set; }

    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}


// ======================= DTOs ============================

// DTO para requisição de Produto (não inclui o Id)
public class ProdutoRequest
{
    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    public string Nome { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Preco { get; set; }

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public int CategoriaId { get; set; }
}

// DTO para resposta de Produto (inclui o Id)
public class ProdutoResponse
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public int CategoriaId { get; set; }
    public CategoriaResponse? Categoria { get; set; }
}

// DTO para requisição de Categoria (não inclui o Id)
public class CategoriaRequest
{
    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    public string Nome { get; set; }
    public string Descricao { get; set; }
}

// DTO para resposta de Categoria (inclui o Id)
public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
}


// ======================= CONTROLLER DE PRODUTOS ============================
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProdutosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/produtos
    [HttpGet]
    public async Task<IActionResult> GetProdutos()
    {
        var produtos = await _context.Produtos
            .Include(p => p.Categoria)
            .ToListAsync();

        var response = produtos.Select(p => new ProdutoResponse
        {
            Id = p.Id,
            Nome = p.Nome,
            Preco = p.Preco,
            CategoriaId = p.CategoriaId,
            Categoria = p.Categoria == null ? null : new CategoriaResponse
            {
                Id = p.Categoria.Id,
                Nome = p.Categoria.Nome,
                Descricao = p.Categoria.Descricao
            }
        });

        return Ok(response);
    }

    // GET: api/produtos/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProdutoById(int id)
    {
        var produto = await _context.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null)
            return NotFound($"Produto com ID {id} não encontrado.");

        var response = new ProdutoResponse
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Preco = produto.Preco,
            CategoriaId = produto.CategoriaId,
            Categoria = produto.Categoria == null ? null : new CategoriaResponse
            {
                Id = produto.Categoria.Id,
                Nome = produto.Categoria.Nome,
                Descricao = produto.Categoria.Descricao
            }
        };

        return Ok(response);
    }

    // POST: api/produtos
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProdutoRequest produtoRequest)
    {
        if (produtoRequest == null)
            return BadRequest("O produto não pode ser nulo.");

        // Verifica se a categoria existe
        var categoria = await _context.Categorias.FindAsync(produtoRequest.CategoriaId);
        if (categoria == null)
            return BadRequest($"A categoria com ID {produtoRequest.CategoriaId} não existe.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var produto = new Produto
        {
            Nome = produtoRequest.Nome,
            Preco = produtoRequest.Preco,
            CategoriaId = produtoRequest.CategoriaId
        };

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        var response = new ProdutoResponse
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Preco = produto.Preco,
            CategoriaId = produto.CategoriaId,
            Categoria = new CategoriaResponse
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                Descricao = categoria.Descricao
            }
        };

        return CreatedAtAction(nameof(GetProdutoById), new { id = produto.Id }, response);
    }

    // PUT: api/produtos/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProdutoRequest produtoRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound($"Produto com ID {id} não encontrado.");

        // Verifica se a nova categoria existe
        var categoria = await _context.Categorias.FindAsync(produtoRequest.CategoriaId);
        if (categoria == null)
            return BadRequest($"A categoria com ID {produtoRequest.CategoriaId} não existe.");

        produto.Nome = produtoRequest.Nome;
        produto.Preco = produtoRequest.Preco;
        produto.CategoriaId = produtoRequest.CategoriaId;

        _context.Produtos.Update(produto);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/produtos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound($"Produto com ID {id} não encontrado.");

        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


// ======================= CONTROLLER DE CATEGORIAS ============================
[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/categorias
    [HttpGet]
    public async Task<IActionResult> GetCategorias()
    {
        var categorias = await _context.Categorias.ToListAsync();
        var response = categorias.Select(c => new CategoriaResponse
        {
            Id = c.Id,
            Nome = c.Nome,
            Descricao = c.Descricao
        });
        return Ok(response);
    }

    // GET: api/categorias/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoriaById(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
            return NotFound($"Categoria com ID {id} não encontrada.");

        var response = new CategoriaResponse
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Descricao = categoria.Descricao
        };

        return Ok(response);
    }

    // POST: api/categorias
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoriaRequest categoriaRequest)
    {
        if (categoriaRequest == null)
            return BadRequest("A categoria não pode ser nula.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var categoria = new Categoria
        {
            Nome = categoriaRequest.Nome,
            Descricao = categoriaRequest.Descricao
        };

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        var response = new CategoriaResponse
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Descricao = categoria.Descricao
        };

        return CreatedAtAction(nameof(GetCategoriaById), new { id = categoria.Id }, response);
    }

    // PUT: api/categorias/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaRequest categoriaRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
            return NotFound($"Categoria com ID {id} não encontrada.");

        categoria.Nome = categoriaRequest.Nome;
        categoria.Descricao = categoriaRequest.Descricao;

        _context.Categorias.Update(categoria);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categorias/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
            return NotFound($"Categoria com ID {id} não encontrada.");

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
