﻿using _6.NovaPoshta.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("tbl_departments")]
public class DepartmentEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Ref { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string CityRef { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [ForeignKey("City")]
    public int CityId { get; set; }
    public CityEntity? City { get; set; }

 
}