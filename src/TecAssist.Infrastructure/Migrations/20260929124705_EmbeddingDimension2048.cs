using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace TecAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmbeddingDimension2048 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("drop index if exists ix_document_chunks_embedding_hnsw;");

            migrationBuilder.AlterColumn<Vector>(
                name: "embedding",
                table: "document_chunks",
                type: "vector(2048)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(1024)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "embedding",
                table: "document_chunks",
                type: "vector(1024)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(2048)",
                oldNullable: true);

            migrationBuilder.Sql("create index if not exists ix_document_chunks_embedding_hnsw on document_chunks using hnsw (embedding vector_cosine_ops);");
        }
    }
}
