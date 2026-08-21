import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { ProductService } from '../services/product.service';
import { ProductsComponent } from './products.component';

describe('ProductsComponent', () => {
  let component: ProductsComponent;
  let fixture: ComponentFixture<ProductsComponent>;
  let productService: jasmine.SpyObj<ProductService>;

  beforeEach(async () => {
    productService = jasmine.createSpyObj<ProductService>(
      'ProductService',
      ['getAll', 'create', 'update', 'delete']
    );
    productService.getAll.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [ProductsComponent],
      providers: [
        { provide: ProductService, useValue: productService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create a valid product and add it to the list', () => {
    const createdProduct = { id: 1, name: 'Notebook', price: 3500 };
    productService.create.and.returnValue(of(createdProduct));
    component.productForm.setValue({ name: ' Notebook ', price: 3500 });

    component.saveProduct();

    expect(productService.create).toHaveBeenCalledWith({
      name: 'Notebook',
      price: 3500
    });
    expect(component.products).toEqual([createdProduct]);
    expect(component.formSuccessMessage).toContain('cadastrado com sucesso');
  });

  it('should update the selected product in the list', () => {
    const product = { id: 7, name: 'Mouse', price: 100 };
    component.products = [product];
    component.startEditing(product);
    component.productForm.setValue({ name: 'Mouse Gamer', price: 180 });
    productService.update.and.returnValue(of(void 0));

    component.saveProduct();

    expect(productService.update).toHaveBeenCalledWith(7, {
      name: 'Mouse Gamer',
      price: 180
    });
    expect(component.products).toEqual([
      { id: 7, name: 'Mouse Gamer', price: 180 }
    ]);
    expect(component.isEditing).toBeFalse();
  });

  it('should delete a product after confirmation', () => {
    const product = { id: 9, name: 'Teclado', price: 250 };
    component.products = [product];
    productService.delete.and.returnValue(of(void 0));
    spyOn(window, 'confirm').and.returnValue(true);

    component.deleteProduct(product);

    expect(productService.delete).toHaveBeenCalledWith(9);
    expect(component.products).toEqual([]);
    expect(component.catalogSuccessMessage).toContain('excluído com sucesso');
  });

  it('should preserve current products when a refresh fails', () => {
    const currentProducts = [{ id: 3, name: 'Monitor', price: 900 }];
    const refreshResult = new Subject<typeof currentProducts>();
    component.products = currentProducts;
    component.hasLoadedProducts = true;
    productService.getAll.and.returnValue(refreshResult);

    component.loadProducts();

    expect(component.isLoading).toBeFalse();
    expect(component.isRefreshing).toBeTrue();
    expect(component.products).toEqual(currentProducts);

    refreshResult.error(new Error('API unavailable'));

    expect(component.isRefreshing).toBeFalse();
    expect(component.products).toEqual(currentProducts);
    expect(component.catalogErrorMessage).toContain('dados anteriores foram mantidos');
  });
});
