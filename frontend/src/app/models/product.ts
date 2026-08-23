export interface Product {
  readonly id: number;
  readonly name: string;
  readonly price: number;
}

export interface CreateProductRequest {
  readonly name: string;
  readonly price: number;
}

export interface UpdateProductRequest {
  readonly name: string;
  readonly price: number;
}
