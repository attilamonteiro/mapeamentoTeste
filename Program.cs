using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
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

// Configuração de Middleware
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
        modelBuilder.ApplyConfiguration(new ProdutoConfiguration());
        modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
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
    public int Id { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; }
}

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}

// ======================= CONTROLLER ============================
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
        return Ok(produtos);
    }

    // GET: api/produtos/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProdutoById(int id)
    {
        var produto = await _context.Produtos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null)
        {
            return NotFound($"Produto com ID {id} não encontrado.");
        }

        return Ok(produto);
    }

    // POST: api/produtos
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Produto produto)
    {
        if (produto == null)
        {
            return BadRequest("O produto não pode ser nulo.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProdutoById), new { id = produto.Id }, produto);
    }

    // PUT: api/produtos/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Produto produtoAtualizado)
    {
        if (id != produtoAtualizado.Id)
        {
            return BadRequest("O ID do produto não corresponde.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var produtoExistente = await _context.Produtos.FindAsync(id);
        if (produtoExistente == null)
        {
            return NotFound($"Produto com ID {id} não encontrado.");
        }

        produtoExistente.Nome = produtoAtualizado.Nome;
        produtoExistente.Preco = produtoAtualizado.Preco;
        produtoExistente.CategoriaId = produtoAtualizado.CategoriaId;

        _context.Produtos.Update(produtoExistente);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/produtos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
        {
            return NotFound($"Produto com ID {id} não encontrado.");
        }

        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
