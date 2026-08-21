import { CurrencyPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { finalize } from 'rxjs';
import { Product } from '../models/product';
import { ProductService } from '../services/product.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CurrencyPipe, ReactiveFormsModule],
  templateUrl: './products.component.html',
  styleUrl: './products.component.css'
})
export class ProductsComponent implements OnInit {
  readonly productForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.pattern(/\S/),
        Validators.maxLength(120)
      ]
    }),
    price: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(0.01)]
    })
  });

  products: Product[] = [];
  isLoading = false;
  errorMessage = '';
  hasAttemptedSubmit = false;

  constructor(private readonly productService: ProductService) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  get nameControl(): FormControl<string> {
    return this.productForm.controls.name;
  }

  get priceControl(): FormControl<number | null> {
    return this.productForm.controls.price;
  }

  validateProductForm(): void {
    this.hasAttemptedSubmit = true;
    this.productForm.markAllAsTouched();
  }

  resetProductForm(): void {
    this.productForm.reset({ name: '', price: null });
    this.hasAttemptedSubmit = false;
  }

  loadProducts(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.productService.getAll()
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: products => this.products = products,
        error: () => {
          this.errorMessage =
            'Não foi possível carregar os produtos. Verifique se o backend está online.';
        }
      });
  }
}
