﻿// <auto-generated />
using System;
using GSheetTelegramBot.DataLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GSheetTelegramBot.DataLayer.Migrations
{
    [DbContext(typeof(GSheetTelegramBotDbContext))]
    partial class GSheetTelegramBotDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.27")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.ChangeNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("CellName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ChangeTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("ColumnName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GoogleSheetId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("GoogleTableId")
                        .HasColumnType("int");

                    b.Property<string>("Hyperlink")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NewValue")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValue")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SheetName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TableName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("GoogleTableId");

                    b.ToTable("ChangeNotification");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.GoogleTable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("GoogleSheetId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("HyperLink")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("GoogleTables");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.Subscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool>("DailySummary")
                        .HasColumnType("bit");

                    b.Property<string>("GoogleSheetId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("InstantNotifications")
                        .HasColumnType("bit");

                    b.Property<string>("TableName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<TimeSpan>("DailySummaryTime")
                        .HasColumnType("time");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EmailConfirmationToken")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EmailConfirmationTokenCreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsAwaitingEmailConfirmation")
                        .HasColumnType("bit");

                    b.Property<bool>("IsAwaitingEmailInput")
                        .HasColumnType("bit");

                    b.Property<bool>("IsEmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<DateTime>("RegisteredAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.Property<long>("TelegramId")
                        .HasColumnType("bigint");

                    b.Property<string>("TimeZoneId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.ChangeNotification", b =>
                {
                    b.HasOne("GSheetTelegramBot.DataLayer.DbModels.GoogleTable", null)
                        .WithMany("DailyChanges")
                        .HasForeignKey("GoogleTableId");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.Subscription", b =>
                {
                    b.HasOne("GSheetTelegramBot.DataLayer.DbModels.User", "User")
                        .WithMany("Subscriptions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.GoogleTable", b =>
                {
                    b.Navigation("DailyChanges");
                });

            modelBuilder.Entity("GSheetTelegramBot.DataLayer.DbModels.User", b =>
                {
                    b.Navigation("Subscriptions");
                });
#pragma warning restore 612, 618
        }
    }
}
