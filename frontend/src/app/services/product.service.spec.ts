import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpTesting: HttpTestingController;
  const productsUrl = `${environment.apiUrl}/products`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProductService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ProductService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('should get all products', () => {
    const products = [{ id: 1, name: 'Notebook', price: 3500 }];

    service.getAll().subscribe(response => expect(response).toEqual(products));

    const request = httpTesting.expectOne(productsUrl);
    expect(request.request.method).toBe('GET');
    request.flush(products);
  });

  it('should get a product by id', () => {
    const product = { id: 4, name: 'Monitor', price: 900 };

    service.getById(4).subscribe(response => expect(response).toEqual(product));

    const request = httpTesting.expectOne(`${productsUrl}/4`);
    expect(request.request.method).toBe('GET');
    request.flush(product);
  });

  it('should create a product', () => {
    const body = { name: 'Teclado', price: 250 };
    const createdProduct = { id: 5, ...body };

    service.create(body).subscribe(response =>
      expect(response).toEqual(createdProduct)
    );

    const request = httpTesting.expectOne(productsUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(createdProduct);
  });

  it('should update a product', () => {
    const body = { name: 'Teclado Mecânico', price: 320 };

    service.update(5, body).subscribe(response =>
      expect(response).toBeNull()
    );

    const request = httpTesting.expectOne(`${productsUrl}/5`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush(null);
  });

  it('should delete a product', () => {
    service.delete(5).subscribe(response => expect(response).toBeNull());

    const request = httpTesting.expectOne(`${productsUrl}/5`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
