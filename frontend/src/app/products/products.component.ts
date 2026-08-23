import { CurrencyPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { finalize } from 'rxjs';
import {
  CreateProductRequest,
  Product,
  UpdateProductRequest
} from '../models/product';
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
  isRefreshing = false;
  isSubmitting = false;
  hasLoadedProducts = false;
  editingProductId: number | null = null;
  deletingProductId: number | null = null;
  errorMessage = '';
  formSuccessMessage = '';
  formErrorMessage = '';
  catalogSuccessMessage = '';
  catalogErrorMessage = '';
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

  get isEditing(): boolean {
    return this.editingProductId !== null;
  }

  get isInterfaceBusy(): boolean {
    return this.isLoading
      || this.isRefreshing
      || this.isSubmitting
      || this.deletingProductId !== null;
  }

  saveProduct(): void {
    if (this.isInterfaceBusy) {
      return;
    }

    this.hasAttemptedSubmit = true;
    this.productForm.markAllAsTouched();
    this.formSuccessMessage = '';
    this.formErrorMessage = '';

    const price = this.priceControl.value;

    if (this.productForm.invalid || price === null) {
      return;
    }

    const request = {
      name: this.nameControl.value.trim(),
      price
    };

    if (this.editingProductId === null) {
      this.createProduct(request);
      return;
    }

    this.updateProduct(this.editingProductId, request);
  }

  startEditing(product: Product): void {
    this.editingProductId = product.id;
    this.productForm.setValue({
      name: product.name,
      price: product.price
    });
    this.hasAttemptedSubmit = false;
    this.formSuccessMessage = '';
    this.formErrorMessage = '';
  }

  resetProductForm(): void {
    this.editingProductId = null;
    this.productForm.reset({ name: '', price: null });
    this.hasAttemptedSubmit = false;
    this.formSuccessMessage = '';
    this.formErrorMessage = '';
  }

  deleteProduct(product: Product): void {
    if (this.isInterfaceBusy) {
      return;
    }

    const confirmed = window.confirm(
      `Deseja realmente excluir o produto "${product.name}"?`
    );

    if (!confirmed) {
      return;
    }

    this.catalogSuccessMessage = '';
    this.catalogErrorMessage = '';
    this.deletingProductId = product.id;

    this.productService.delete(product.id)
      .pipe(finalize(() => this.deletingProductId = null))
      .subscribe({
        next: () => {
          this.products = this.products.filter(item => item.id !== product.id);
          this.catalogSuccessMessage = `Produto "${product.name}" excluído com sucesso.`;

          if (this.editingProductId === product.id) {
            this.resetProductForm();
          }
        },
        error: () => {
          this.catalogErrorMessage =
            'Não foi possível excluir o produto. Tente novamente.';
        }
      });
  }

  private createProduct(request: CreateProductRequest): void {
    this.isSubmitting = true;

    this.productService.create(request)
      .pipe(finalize(() => this.isSubmitting = false))
      .subscribe({
        next: product => {
          this.products = [...this.products, product];
          this.errorMessage = '';
          this.productForm.reset({ name: '', price: null });
          this.hasAttemptedSubmit = false;
          this.formSuccessMessage = `Produto "${product.name}" cadastrado com sucesso.`;
        },
        error: () => {
          this.formErrorMessage =
            'Não foi possível cadastrar o produto. Verifique os dados e tente novamente.';
        }
      });
  }

  private updateProduct(id: number, request: UpdateProductRequest): void {
    this.isSubmitting = true;

    this.productService.update(id, request)
      .pipe(finalize(() => this.isSubmitting = false))
      .subscribe({
        next: () => {
          this.products = this.products.map(product =>
            product.id === id ? { id, ...request } : product
          );
          this.editingProductId = null;
          this.productForm.reset({ name: '', price: null });
          this.hasAttemptedSubmit = false;
          this.formSuccessMessage = `Produto "${request.name}" atualizado com sucesso.`;
        },
        error: () => {
          this.formErrorMessage =
            'Não foi possível atualizar o produto. Verifique os dados e tente novamente.';
        }
      });
  }

  loadProducts(): void {
    if (this.isInterfaceBusy) {
      return;
    }

    const isInitialLoad = !this.hasLoadedProducts;

    this.isLoading = isInitialLoad;
    this.isRefreshing = !isInitialLoad;
    this.errorMessage = '';
    this.catalogSuccessMessage = '';
    this.catalogErrorMessage = '';

    this.productService.getAll()
      .pipe(finalize(() => {
        this.isLoading = false;
        this.isRefreshing = false;
      }))
      .subscribe({
        next: products => {
          this.products = products;
          this.hasLoadedProducts = true;
        },
        error: () => {
          if (isInitialLoad) {
            this.errorMessage =
              'Não foi possível carregar os produtos. Verifique se o backend está online.';
            return;
          }

          this.catalogErrorMessage =
            'Não foi possível atualizar a lista. Os dados anteriores foram mantidos.';
        }
      });
  }
}
