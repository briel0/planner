import { HttpErrorResponse } from '@angular/common/http';
import { call } from './http-planner-data';
import { PlannerDataError } from './planner-data';

describe('call (HTTP errors → PlannerDataError)', () => {
  const failWith = (response: HttpErrorResponse) => call(() => Promise.reject(response));

  it('keeps the stable code from the Problem Details', async () => {
    const response = new HttpErrorResponse({
      status: 409,
      error: { code: 'category.name-taken', title: 'A category with this name already exists.' },
    });

    await expect(failWith(response)).rejects.toMatchObject({ code: 'category.name-taken', status: 409 });
  });

  it('reports an unreachable server as a network error', async () => {
    const error = await failWith(new HttpErrorResponse({ status: 0 })).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(PlannerDataError);
    expect(error).toMatchObject({ code: 'network' });
  });
});
