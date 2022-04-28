﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using binaire;

#nullable disable

namespace binaire.Migrations
{
    [DbContext(typeof(Database.binaireDbContext))]
    [Migration("20220428081349_initialMigration")]
    partial class initialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("binaire.Board", b =>
                {
                    b.Property<int>("BoardId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BoardId1")
                        .HasColumnType("int");

                    b.Property<int>("BoardId2")
                        .HasColumnType("int");

                    b.Property<int>("BoardId3")
                        .HasColumnType("int");

                    b.Property<int>("BoardSpecifier")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("BoardId");

                    b.ToTable("Boards");
                });

            modelBuilder.Entity("binaire.Reading", b =>
                {
                    b.Property<int>("ReadingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("BoardId")
                        .HasColumnType("int");

                    b.Property<byte[]>("Fingerprint")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("longblob");

                    b.Property<int>("PufEnd")
                        .HasColumnType("int");

                    b.Property<int>("PufStart")
                        .HasColumnType("int");

                    b.Property<float>("Temperature")
                        .HasColumnType("float");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.HasKey("ReadingId");

                    b.HasIndex("BoardId");

                    b.ToTable("Readings");
                });

            modelBuilder.Entity("binaire.Reading", b =>
                {
                    b.HasOne("binaire.Board", "Board")
                        .WithMany()
                        .HasForeignKey("BoardId");

                    b.Navigation("Board");
                });
#pragma warning restore 612, 618
        }
    }
}
