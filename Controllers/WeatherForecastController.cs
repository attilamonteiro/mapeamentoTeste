//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//[ApiController]
//[Route("api/[controller]")]
//public class ProdutosController : ControllerBase
//{
//    private readonly AppDbContext _context;

//    public ProdutosController(AppDbContext context)
//    {
//        _context = context;
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetProdutos()
//    {
//        var produtos = await _context.Produtos
//            .Include(p => p.Categoria)
//            .ToListAsync();
//        return Ok(produtos);
//    }

//    [HttpPost]
//    public async Task<IActionResult> Create([FromBody] Produto produto)
//    {
//        Console.WriteLine($"Recebido: {System.Text.Json.JsonSerializer.Serialize(produto)}");
//        if (produto == null)
//        {
//            return BadRequest("O produto não pode ser nulo.");
//        }

//        if (!ModelState.IsValid)
//        {
//            Console.WriteLine("ModelState inválido:");
//            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
//            {
//                Console.WriteLine(error.ErrorMessage);
//            }
//            return BadRequest(ModelState);
//        }

//        _context.Produtos.Add(produto);
//        await _context.SaveChangesAsync();

//        return CreatedAtAction(nameof(GetProdutos), new { id = produto.Id }, produto);
//    }
//}